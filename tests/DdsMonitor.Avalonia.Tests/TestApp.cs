using Avalonia;
using Avalonia.Headless;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Themes.Fluent;

[assembly: AvaloniaTestApplication(typeof(DdsMonitor.Avalonia.Tests.TestApp))]

namespace DdsMonitor.Avalonia.Tests;

/// <summary>
/// Minimal Avalonia application for headless shell tests.
/// Provides the FluentTheme, the DdsMonitor design tokens, and control styles
/// so controls and DesignTokensTests can resolve all custom resource keys.
/// </summary>
public sealed class TestApp : Application
{
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<TestApp>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions());

    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
        Styles.Add(new StyleInclude(
            new Uri("avares://DdsMonitor.Avalonia/Themes/BaseStyles.axaml"))
        {
            Source = new Uri("avares://DdsMonitor.Avalonia/Themes/BaseStyles.axaml"),
        });
        Styles.Add(new StyleInclude(
            new Uri("avares://DdsMonitor.Avalonia/Controls/MdiChild.axaml"))
        {
            Source = new Uri("avares://DdsMonitor.Avalonia/Controls/MdiChild.axaml"),
        });
        Styles.Add(new StyleInclude(
            new Uri("avares://DdsMonitor.Avalonia/Controls/MdiHost.axaml"))
        {
            Source = new Uri("avares://DdsMonitor.Avalonia/Controls/MdiHost.axaml"),
        });
        Resources.MergedDictionaries.Add(new ResourceInclude(
            new Uri("avares://DdsMonitor.Avalonia/Themes/DesignTokens.axaml"))
        {
            Source = new Uri("avares://DdsMonitor.Avalonia/Themes/DesignTokens.axaml"),
        });
    }
}
