using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using DdsMonitor.Avalonia.Controls;
using DdsMonitor.Avalonia.Core;
using DdsMonitor.Avalonia.Docking;
using DdsMonitor.Engine;
using DdsMonitor.Engine.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace DdsMonitor.Avalonia;

public sealed partial class ShellWindow : Window
{
    private readonly IDdsBridge? _ddsBridge;
    private readonly IMenuRegistry? _menuRegistry;
    private readonly IThemeService? _themeService;

    public ShellWindow(IServiceProvider services)
    {
        InitializeComponent();

        // 1. Create the MDI host that lives inside the central DockControl document
        var mdiHost = new MdiHost { Width = 800, Height = 600 };

        // 2. Resolve IDockManager → wire it to the DockControl and MDI host
        var dockManager = services.GetService<IDockManager>();
        dockManager?.Initialise(MainDock, mdiHost);

        // 3. Resolve IAvaloniaWindowManager → give it the MDI host and dock manager
        var windowManager = services.GetService<IAvaloniaWindowManager>();
        if (windowManager is not null)
        {
            windowManager.SetMdiHost(mdiHost);
            if (dockManager is not null)
                windowManager.SetDockManager(dockManager);
        }

        // 4. Rebuild the Plugins menu from IMenuRegistry; re-build on change
        _menuRegistry = services.GetService<IMenuRegistry>();
        if (_menuRegistry is not null)
        {
            RebuildPluginsMenu();
            _menuRegistry.Changed += RebuildPluginsMenu;
        }

        // 5. Wire the transport buttons to IDdsBridge
        _ddsBridge = services.GetService<IDdsBridge>();
        PlayButton.Click  += (_, _) => { if (_ddsBridge is not null) _ddsBridge.IsPaused = false; };
        PauseButton.Click += (_, _) => { if (_ddsBridge is not null) _ddsBridge.IsPaused = true; };
        ResetButton.Click += (_, _) => _ddsBridge?.ResetAll();

        // Wire Exit menu item
        ExitItem.Click += (_, _) => Close();

        // 6. Wire Theme submenu
        _themeService = services.GetService<IThemeService>();
        ThemeSystemItem.Click += (_, _) => _themeService?.SetMode(ThemeMode.System);
        ThemeLightItem.Click  += (_, _) => _themeService?.SetMode(ThemeMode.Light);
        ThemeDarkItem.Click   += (_, _) => _themeService?.SetMode(ThemeMode.Dark);

        // 7. Register keyboard shortcuts via IKeyboardShortcutService
        var shortcutService = services.GetService<IKeyboardShortcutService>();
        if (shortcutService is not null)
        {
            shortcutService.Register(
                new KeyGesture(Key.Space), "Play/Pause",
                () => { if (_ddsBridge is not null) _ddsBridge.IsPaused = !_ddsBridge.IsPaused; });
            shortcutService.Register(
                new KeyGesture(Key.F5), "Reset",
                () => _ddsBridge?.ResetAll());
        }

        // 8. Start 1 Hz DispatcherTimer for status-area updates
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        timer.Tick += OnStatusTick;
        timer.Start();
    }

    // ── Plugins menu ──────────────────────────────────────────────────────────

    private void RebuildPluginsMenu()
    {
        PluginsMenu.Items.Clear();
        if (_menuRegistry is null) return;

        foreach (var node in _menuRegistry.GetTopLevelMenus())
            PluginsMenu.Items.Add(BuildMenuItem(node));
    }

    private static MenuItem BuildMenuItem(MenuNode node)
    {
        var item = new MenuItem { Header = node.Label };

        if (node.IsLeaf)
        {
            item.Click += (_, _) =>
            {
                if (node.OnClickAsync is not null)
                    _ = node.OnClickAsync();
                else
                    node.OnClick?.Invoke();
            };
        }
        else
        {
            foreach (var child in node.Children)
                item.Items.Add(BuildMenuItem(child));
        }

        return item;
    }

    // ── Status ticker ─────────────────────────────────────────────────────────

    private void OnStatusTick(object? sender, EventArgs e)
    {
        if (_ddsBridge is null) return;

        StatusText.Text = _ddsBridge.IsPaused ? "Paused" : "Running";
        StatusDot.Fill  = _ddsBridge.IsPaused
            ? Brushes.Orange
            : Brushes.Green;

        // Bandwidth summary — detailed stats deferred to M2
        BandwidthText.Text = FormatBandwidth(0);
    }

    // ── Bandwidth formatter ───────────────────────────────────────────────────

    internal static string FormatBandwidth(long bps)
    {
        if (bps <= 0) return "0 B/s";
        if (bps < 1024) return $"{bps} B/s";
        if (bps < 1_048_576) return $"{(bps / 1024.0).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)} KB/s";
        return $"{(bps / 1_048_576.0).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)} MB/s";
    }
}
