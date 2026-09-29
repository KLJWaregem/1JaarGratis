using EenJaarGratis.Services;
using Microsoft.AspNetCore.Components;

namespace EenJaarGratis.Components.Shared;

/// <summary>
/// Base class for pages that follow the live show. Subscribes to GameState,
/// reloads and re-renders when something changes, and unsubscribes when the
/// tab closes. This is what replaces all the signalR.on(...)/off(...) code.
/// </summary>
public abstract class LiveComponent : ComponentBase, IDisposable
{
    [Inject] protected GameState State { get; set; } = null!;

    protected override async Task OnInitializedAsync()
    {
        State.Changed += OnStateChanged;
        await ReloadAsync();
    }

    /// <summary>Load whatever this page needs from the database.</summary>
    protected virtual Task ReloadAsync() => Task.CompletedTask;

    // Changed is raised from another user's circuit, so hop onto our own.
    private void OnStateChanged() => _ = InvokeAsync(async () =>
    {
        await ReloadAsync();
        StateHasChanged();
    });

    public virtual void Dispose() => State.Changed -= OnStateChanged;
}
