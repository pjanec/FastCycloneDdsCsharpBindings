using DdsMonitor.Avalonia.Core;
using DdsMonitor.Engine;
using DdsMonitor.Engine.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace DdsMonitor.Avalonia.FeatureDemoPlugin;

/// <summary>
/// Feature Demo plugin — illustrates all five DDS topic types and hosts
/// a live dashboard panel.
/// </summary>
public sealed class FeatureDemoPlugin : IMonitorPlugin
{
    public string Name    => "FeatureDemo";
    public string Version => "1.0";

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<DemoPublisherService>();
        services.AddHostedService(sp => sp.GetRequiredService<DemoPublisherService>());
    }

    public void Initialize(IMonitorContext context)
    {
        var viewRegistry = context.GetFeature<IAvaloniaViewRegistry>();
        var windowManager = context.GetFeature<IWindowManager>();
        var menuRegistry  = context.GetFeature<IMenuRegistry>();
        var publisher     = context.GetFeature<DemoPublisherService>();

        // Register the view factory for the dashboard panel.
        viewRegistry?.Register<FeatureDemoDashboardViewModel>(
            vm => new FeatureDemoDashboardView { DataContext = vm });

        // Devel → Feature Demo Toggle Publisher
        if (menuRegistry is not null && publisher is not null)
        {
            menuRegistry.AddMenuItem("Devel", "_Feature Demo Toggle Publisher", () =>
                publisher.ToggleEnabled());
        }

        // View → Feature Demo Dashboard
        if (menuRegistry is not null && windowManager is not null)
        {
            menuRegistry.AddMenuItem("View", "Feature _Demo Dashboard", () =>
                windowManager.SpawnPanel(nameof(FeatureDemoDashboardViewModel), null));
        }
    }
}
