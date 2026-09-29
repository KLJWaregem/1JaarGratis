using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using EenJaarGratis.Data;
using Microsoft.EntityFrameworkCore;

namespace EenJaarGratis.Services;

public record ScoreboardRow(int Id, string Name, int Points);
public record ScanResult(bool Ok, string Message);

/// <summary>
/// Everything the pages need, in one place. Replaces the controllers,
/// MediatR handlers, AutoMapper profile, repositories and the JS gateways.
/// Uses a DbContext *factory*: in Blazor Server a component lives as long as
/// the browser tab, so each operation gets its own short-lived context.
/// </summary>
public class QuizService(IDbContextFactory<AppDbContext> dbFactory, GameState state)
{
    private static readonly SemaphoreSlim RandomLock = new(1, 1);

    // ---------- Questions ----------

    public async Task<List<Question>> GetQuestionsAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Questions.AsNoTracking().OrderBy(q => q.SortOrder).ThenBy(q => q.Id).ToListAsync();
    }

    public async Task<Question?> GetQuestionAsync(int id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Questions.AsNoTracking().SingleOrDefaultAsync(q => q.Id == id);
    }

    public async Task SaveQuestionAsync(Question question)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (question.Id == 0)
        {
            var maxOrder = await db.Questions.Select(q => (int?)q.SortOrder).MaxAsync() ?? -1;
            question.SortOrder = maxOrder + 1;
            db.Questions.Add(question);
        }
        else
        {
            db.Questions.Update(question);
        }
        await db.SaveChangesAsync();
        state.NotifyChanged();
    }

    public async Task DeleteQuestionAsync(int id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await db.Questions.Where(q => q.Id == id).ExecuteDeleteAsync();
        state.NotifyChanged();
    }

    /// <summary>Swaps this question's SortOrder with its neighbour (direction -1 = up, +1 = down).</summary>
    public async Task MoveQuestionAsync(int id, int direction)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var questions = await db.Questions.OrderBy(q => q.SortOrder).ThenBy(q => q.Id).ToListAsync();
        int index = questions.FindIndex(q => q.Id == id);
        int neighbourIndex = index + direction;
        if (index < 0 || neighbourIndex < 0 || neighbourIndex >= questions.Count) return;

        (questions[index].SortOrder, questions[neighbourIndex].SortOrder) =
            (questions[neighbourIndex].SortOrder, questions[index].SortOrder);
        await db.SaveChangesAsync();
        state.NotifyChanged();
    }

    public async Task ImportCsvAsync(string csv)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = ";" };
        using var reader = new CsvReader(new StringReader(csv), config);

        await using var db = await dbFactory.CreateDbContextAsync();
        var nextOrder = 1 + (await db.Questions.Select(q => (int?)q.SortOrder).MaxAsync() ?? -1);
        foreach (var row in reader.GetRecords<CsvQuestion>())
        {
            // First column in the CSV is the correct answer; shuffle for display.
            string[] answers = [row.Antwoord1, row.Antwoord2, row.Antwoord3];
            Random.Shared.Shuffle(answers);
            db.Questions.Add(new Question
            {
                QuestionText = row.Vraag,
                Answer1 = answers[0], Answer2 = answers[1], Answer3 = answers[2],
                CorrectAnswer = Array.IndexOf(answers, row.Antwoord1),
                SortOrder = nextOrder++,
            });
        }
        await db.SaveChangesAsync();   // one transaction for the whole file
    }

    private record CsvQuestion(string Vraag, string Antwoord1, string Antwoord2, string Antwoord3);

    // ---------- Groups ----------

    public async Task<List<QuestionGroup>> GetGroupsAsync(int questionId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.QuestionGroups.AsNoTracking()
            .Where(g => g.QuestionId == questionId)
            .Include(g => g.Players)
            .OrderBy(g => g.Id)
            .ToListAsync();
    }

    public async Task<QuestionGroup> CreateGroupAsync(int questionId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var group = new QuestionGroup { QuestionId = questionId };
        db.QuestionGroups.Add(group);
        await db.SaveChangesAsync();
        return group;
    }

    public async Task DeleteGroupAsync(int groupId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await db.QuestionGroups.Where(g => g.Id == groupId).ExecuteDeleteAsync();
        state.NotifyChanged();
    }

    public async Task<ScanResult> AddPlayerByCodeAsync(int groupId, string code)
    {
        await using var db = await dbFactory.CreateDbContextAsync();

        var player = await db.Players.SingleOrDefaultAsync(p => p.Code == code);
        if (player is null) return new(false, "Onbekende QR-code");

        var group = await db.QuestionGroups.Include(g => g.Players)
            .SingleOrDefaultAsync(g => g.Id == groupId);
        if (group is null) return new(false, "Groep bestaat niet meer");

        bool alreadyInAGroup = await db.QuestionGroups
            .AnyAsync(g => g.QuestionId == group.QuestionId && g.Players.Any(p => p.Id == player.Id));
        if (alreadyInAGroup) return new(false, $"{player.Name} zit al in een groep");

        group.Players.Add(player);
        await db.SaveChangesAsync();
        state.NotifyChanged();
        return new(true, player.Name);
    }

    public async Task RemovePlayerFromGroupAsync(int groupId, int playerId)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var group = await db.QuestionGroups.Include(g => g.Players).SingleAsync(g => g.Id == groupId);
        group.Players.RemoveAll(p => p.Id == playerId);
        await db.SaveChangesAsync();
        state.NotifyChanged();
    }

    // ---------- Scoreboard ----------

    public async Task<List<ScoreboardRow>> GetScoreboardAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        var rows = await db.Players.AsNoTracking()
            .Select(p => new
            {
                p.Id,
                p.Name,
                Points = p.PointOffset + p.QuestionGroups.Sum(g => g.Question.PointsToShare / g.Players.Count)
            })
            .OrderByDescending(r => r.Points).ThenBy(r => r.Name)
            .ToListAsync();
        return rows.ConvertAll(r => new ScoreboardRow(r.Id, r.Name, r.Points));
    }
    
    public async Task SelectRandomPlayersAsync(int count = 3)
    {
        await RandomLock.WaitAsync();   // two quizmaster phones pressing at once
        try
        {
            await using var db = await dbFactory.CreateDbContextAsync();
            var players = await db.Players.ToListAsync();
            var picked = players
                .OrderBy(p => p.QuestionCount).ThenBy(_ => Random.Shared.Next())
                .Take(count).ToList();

            picked.ForEach(p => p.QuestionCount++);
            await db.SaveChangesAsync();
            state.SetSelectedPlayers(picked.Select(p => p.Id));
        }
        finally { RandomLock.Release(); }
    }

    // ---------- Players (CRUD pages would use these) ----------

    public async Task<List<Player>> GetPlayersAsync()
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Players.AsNoTracking().OrderBy(p => p.Name).ToListAsync();
    }

    public async Task<Player?> GetPlayerAsync(int id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        return await db.Players.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id);
    }

    public async Task SavePlayerAsync(Player player)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        if (player.Id == 0 && db.Players.Any())
        {
            player.QuestionCount = db.Players.Min(p => p.QuestionCount);
        }
        db.Players.Update(player);
        await db.SaveChangesAsync();
        state.NotifyChanged();
    }

    public async Task DeletePlayerAsync(int id)
    {
        await using var db = await dbFactory.CreateDbContextAsync();
        await db.Players.Where(p => p.Id == id).ExecuteDeleteAsync();
        state.NotifyChanged();
    }
}
