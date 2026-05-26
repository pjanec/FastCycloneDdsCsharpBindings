using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using DdsMonitor.Avalonia.Core;
using DdsMonitor.Engine;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DdsMonitor.Avalonia.Tests;

/// <summary>
/// Tests for the new ShellWindow (IServiceProvider constructor).
/// Uses ShellTestFactory and stubs defined in ShellTests.cs (same namespace/assembly).
/// </summary>
public sealed class ShellWindowNewTests
{
    [AvaloniaFact]
    public void ShellWindow_HasFourTopLevelMenuItems()
    {
        var (window, _, _, _) = ShellTestFactory.CreateShell();
        var menu = window.FindControl<Menu>("MainMenu");
        Assert.NotNull(menu);
        Assert.Equal(4, menu!.Items.Count);
    }

    [AvaloniaFact]
    public void TransportButtons_HaveToolTips()
    {
        var (window, _, _, _) = ShellTestFactory.CreateShell();
        var play  = window.FindControl<Button>("PlayButton");
        var pause = window.FindControl<Button>("PauseButton");
        var reset = window.FindControl<Button>("ResetButton");
        Assert.NotNull(play);
        Assert.NotNull(pause);
        Assert.NotNull(reset);
        Assert.False(string.IsNullOrEmpty(ToolTip.GetTip(play!)?.ToString()));
        Assert.False(string.IsNullOrEmpty(ToolTip.GetTip(pause!)?.ToString()));
        Assert.False(string.IsNullOrEmpty(ToolTip.GetTip(reset!)?.ToString()));
    }

    [Fact]
    public void BandwidthHelper_FormatsCorrectly()
    {
        Assert.Equal("0 B/s",    ShellWindow.FormatBandwidth(0));
        Assert.Equal("512 B/s",  ShellWindow.FormatBandwidth(512));
        Assert.Equal("1.5 KB/s", ShellWindow.FormatBandwidth(1536));
        Assert.Equal("2.3 MB/s", ShellWindow.FormatBandwidth((long)(2.3 * 1_048_576)));
    }

    [AvaloniaFact]
    public void ShellWindow_ActivePanels_EmptyOnStart()
    {
        var viewRegistry = new AvaloniaViewRegistry();
        var eventBroker  = new StubEventBroker();

        var services = new ServiceCollection()
            .AddSingleton<IAvaloniaViewRegistry>(viewRegistry)
            .AddSingleton<IEventBroker>(eventBroker)
            .AddSingleton<IAvaloniaWindowManager>(sp =>
                new AvaloniaWindowManager(viewRegistry, sp, eventBroker))
            .AddSingleton<IWindowManager>(sp =>
                (IWindowManager)sp.GetRequiredService<IAvaloniaWindowManager>())
            .BuildServiceProvider();

        var window        = new ShellWindow(services);
        var windowManager = services.GetRequiredService<IAvaloniaWindowManager>();

        Assert.Empty(windowManager.ActivePanels);
    }
}
