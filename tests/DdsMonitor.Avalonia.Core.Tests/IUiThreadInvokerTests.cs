using System;
using System.Threading.Tasks;
using DdsMonitor.Avalonia.Core;
using Xunit;

namespace DdsMonitor.Avalonia.Core.Tests;

// ── IUiThreadInvoker ──────────────────────────────────────────────────────────

public sealed class IUiThreadInvokerTests
{
    /// <summary>
    /// Synchronous stub that executes everything inline on the calling thread,
    /// simulating "already on the UI thread".
    /// </summary>
    private sealed class SynchronousInvoker : IUiThreadInvoker
    {
        public bool CheckAccess() => true;
        public void Post(Action action) => action();
        public Task InvokeAsync(Func<Task> action) => action();
    }

    [Fact]
    public void Post_ExecutesAction()
    {
        var invoker = new SynchronousInvoker();
        var executed = false;

        invoker.Post(() => executed = true);

        Assert.True(executed);
    }

    [Fact]
    public async Task InvokeAsync_ExecutesFunc()
    {
        var invoker = new SynchronousInvoker();
        var executed = false;

        await invoker.InvokeAsync(() => { executed = true; return Task.CompletedTask; });

        Assert.True(executed);
    }
}
