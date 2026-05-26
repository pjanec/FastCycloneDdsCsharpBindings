using DdsMonitor.Avalonia.Core;
using DdsMonitor.Avalonia.Docking;
using DdsMonitor.Avalonia.Services;
using DdsMonitor.Engine;
using DdsMonitor.Engine.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DdsMonitor.Avalonia.Tests;

/// <summary>
/// Verifies that every new DI service added in M1-T9 can be resolved from a
/// minimal ServiceCollection that mirrors the real Program.cs registrations.
/// No Avalonia UI thread or headless app required.
/// </summary>
public sealed class ProgramRegistrationTests
{
    [Fact]
    public void AllNewServices_ResolveSuccessfully()
    {
        var viewRegistry = new AvaloniaViewRegistry();
        var eventBroker  = new StubEventBrokerForProgram();

        var services = new ServiceCollection();

        // Stubs for engine dependencies
        services.AddSingleton<IEventBroker>(eventBroker);
        services.AddSingleton<IAvaloniaViewRegistry>(viewRegistry);
        services.AddSingleton<IContextMenuRegistry>(new StubContextMenuRegistryForProgram());
        services.AddSingleton<IUserSettings>(new StubUserSettingsForProgram());

        // New Avalonia services — mirrors Program.cs registrations
        services.AddSingleton<IUiThreadInvoker, AvaloniaUiThreadInvoker>();
        services.AddSingleton<IContextMenuPresenter, ContextMenuPresenter>();
        services.AddSingleton<IFileDialogService>(_ => new FileDialogService(() => null));
        services.AddSingleton<IKeyboardShortcutService, KeyboardShortcutService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<IClipboardService>(_ => new ClipboardService(() => null));
        services.AddSingleton<IDockManager, DockManager>();
        services.AddSingleton<IAvaloniaWindowManager>(sp =>
            new AvaloniaWindowManager(viewRegistry, sp, eventBroker));
        services.AddSingleton<IWindowManager>(sp =>
            (IWindowManager)sp.GetRequiredService<IAvaloniaWindowManager>());

        var sp = services.BuildServiceProvider();

        Assert.NotNull(sp.GetService<IUiThreadInvoker>());
        Assert.NotNull(sp.GetService<IContextMenuPresenter>());
        Assert.NotNull(sp.GetService<IFileDialogService>());
        Assert.NotNull(sp.GetService<IKeyboardShortcutService>());
        Assert.NotNull(sp.GetService<IThemeService>());
        Assert.NotNull(sp.GetService<IClipboardService>());
        Assert.NotNull(sp.GetService<IDockManager>());
        Assert.NotNull(sp.GetService<IWindowManager>());
        Assert.NotNull(sp.GetService<IAvaloniaWindowManager>());

        // IWindowManager and IAvaloniaWindowManager must be the same singleton
        Assert.Same(sp.GetService<IWindowManager>(), sp.GetService<IAvaloniaWindowManager>());
    }

    // ── Local stubs ───────────────────────────────────────────────────────────

    private sealed class StubEventBrokerForProgram : IEventBroker
    {
        public void Publish<TEvent>(TEvent ev) { }
        public IDisposable Subscribe<TEvent>(Action<TEvent> handler) => NoopDisposable.Instance;

        private sealed class NoopDisposable : IDisposable
        {
            public static readonly NoopDisposable Instance = new();
            public void Dispose() { }
        }
    }

    private sealed class StubContextMenuRegistryForProgram : IContextMenuRegistry
    {
        public void RegisterProvider<TContext>(Func<TContext, IEnumerable<ContextMenuItem>> provider) { }
        public IEnumerable<ContextMenuItem> GetItems<TContext>(TContext context) => [];
    }

    private sealed class StubUserSettingsForProgram : IUserSettings
    {
        public T Get<T>(string s, string k, T d) => d;
        public void Set<T>(string s, string k, T v) { }
        public Task SaveAsync() => Task.CompletedTask;
    }
}
