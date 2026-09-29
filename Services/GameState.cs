namespace EenJaarGratis.Services;

/// <summary>What the audience sees on the beamer. Contains no correct answer.</summary>
public record ShownQuestion(int Number, string Text, string A, string B, string C);

/// <summary>
/// The live state of the show, shared by every open screen (singleton).
/// This replaces the SignalR hub: the quizmaster changes it, every component
/// that subscribed to <see cref="Changed"/> re-renders. Blazor Server already
/// pushes those re-renders to the browsers over its own SignalR connection.
/// </summary>
public sealed class GameState
{
    private readonly Lock _lock = new();

    public int CurrentQuestionIndex { get; private set; }
    public ShownQuestion? ShownQuestion { get; private set; }
    public IReadOnlySet<int> SelectedPlayerIds { get; private set; } = new HashSet<int>();

    public event Action? Changed;

    public void GoToQuestion(int index)
    {
        lock (_lock)
        {
            CurrentQuestionIndex = Math.Max(0, index);
            ShownQuestion = null;          // same as the old "hide on next/previous"
            SelectedPlayerIds = new HashSet<int>();
        }
        NotifyChanged();
    }

    public void Show(ShownQuestion question) { lock (_lock) ShownQuestion = question; NotifyChanged(); }
    public void Hide() { lock (_lock) ShownQuestion = null; NotifyChanged(); }

    public void SetSelectedPlayers(IEnumerable<int> ids)
    {
        lock (_lock) SelectedPlayerIds = ids.ToHashSet();
        NotifyChanged();
    }

    /// <summary>Call after any database change that affects what screens show.</summary>
    public void NotifyChanged() => Changed?.Invoke();
}
