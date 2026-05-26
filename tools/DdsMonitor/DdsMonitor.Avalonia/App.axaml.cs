using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DdsMonitor.Engine;
using Microsoft.Extensions.DependencyInjection;

namespace DdsMonitor.Avalonia;

public sealed partial class App : Application
{
    private readonly IServiceProvider _services;

    public App(IServiceProvider services)
    {
        _services = services;
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new ShellWindow(_services);

            // Restore workspace panels now that the UI thread is running and plugins registered.
            var windowManager = _services.GetRequiredService<IWindowManager>();
            var workspaceState = _services.GetRequiredService<IWorkspaceState>();
            windowManager.LoadWorkspace(workspaceState.WorkspaceFilePath);
        }

        base.OnFrameworkInitializationCompleted();
    }
}
