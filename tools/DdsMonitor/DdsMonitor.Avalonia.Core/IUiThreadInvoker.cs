using System;
using System.Threading.Tasks;
using Avalonia.Threading;

namespace DdsMonitor.Avalonia.Core;

/// <summary>
/// Abstracts UI-thread dispatching so that non-Avalonia code (and tests) can
/// post work without a direct dependency on <see cref="Dispatcher"/>.
/// </summary>
public interface IUiThreadInvoker
{
    /// <summary>Returns <c>true</c> if the caller is already on the UI thread.</summary>
    bool CheckAccess();

    /// <summary>Posts <paramref name="action"/> to be executed on the UI thread (fire-and-forget).</summary>
    void Post(Action action);

    /// <summary>Schedules <paramref name="action"/> on the UI thread and returns a <see cref="Task"/> that completes when it finishes.</summary>
    Task InvokeAsync(Func<Task> action);
}

/// <summary>
/// Production implementation that delegates to <see cref="Dispatcher.UIThread"/>.
/// </summary>
public sealed class AvaloniaUiThreadInvoker : IUiThreadInvoker
{
    /// <inheritdoc/>
    public bool CheckAccess() => Dispatcher.UIThread.CheckAccess();

    /// <inheritdoc/>
    public void Post(Action action) =>
        Dispatcher.UIThread.Post(action, DispatcherPriority.Normal);

    /// <inheritdoc/>
    public Task InvokeAsync(Func<Task> action) =>
        Dispatcher.UIThread.InvokeAsync(action);
}
