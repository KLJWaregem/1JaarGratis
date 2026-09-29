namespace EenJaarGratis.Data;

public class Player
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public int PointOffset { get; set; }
    public int QuestionCount { get; set; }
    public List<QuestionGroup> QuestionGroups { get; set; } = [];
}

public class Question
{
    public int Id { get; set; }
    public string QuestionText { get; set; } = "";
    public string Answer1 { get; set; } = "";
    public string Answer2 { get; set; } = "";
    public string Answer3 { get; set; } = "";
    public int CorrectAnswer { get; set; }      // 0, 1 or 2
    public int PointsToShare { get; set; } = 100;
    public int SortOrder { get; set; }          // quiz order; edited from the overview page
    public List<QuestionGroup> QuestionGroups { get; set; } = [];

    public string[] Answers => [Answer1, Answer2, Answer3];
}

public class QuestionGroup
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public Question Question { get; set; } = null!;
    public List<Player> Players { get; set; } = [];
}
