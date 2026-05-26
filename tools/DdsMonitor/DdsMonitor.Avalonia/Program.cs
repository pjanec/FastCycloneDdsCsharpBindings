using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Logging;
using DdsMonitor.Avalonia.Core;
using DdsMonitor.Avalonia.Docking;
using DdsMonitor.Avalonia.Services;
using DdsMonitor.Engine;
using DdsMonitor.Engine.Hosting;
using DdsMonitor.Engine.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DdsMonitor.Avalonia;

internal sealed class Program
{
    // Avalonia requires an STA thread on Windows. Top-level async statements 
    // force MTA, causing PlatformNotSupportedException.
    [STAThread]
    public static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += (s, e) => 
        {
            Console.WriteLine($"[FATAL] AppDomain Unhandled: {e.ExceptionObject}");
        };
    
        TaskScheduler.UnobservedTaskException += (s, e) => 
        {
            Console.WriteLine($"[FATAL] Task Unobserved: {e.Exception}");
        };


        var builder = Host.CreateApplicationBuilder(args);

        // Register core engine services (DDS, plugins, IMenuRegistry, etc.)
        builder.Services.AddDdsMonitorServices(builder.Configuration);

        // Avalonia-specific singletons (registered AFTER engine so these override defaults)
        builder.Services.AddSingleton<IToolbarRegistry, ToolbarRegistry>();
        builder.Services.AddSingleton<IUserSettings, UserSettingsStore>();
        builder.Services.AddSingleton<IAvaloniaViewRegistry, AvaloniaViewRegistry>();
        builder.Services.AddSingleton<IAvaloniaTypeDrawerRegistry, AvaloniaTypeDrawerRegistry>();

        // New Avalonia services (M1-T9)
        builder.Services.AddSingleton<IUiThreadInvoker, AvaloniaUiThreadInvoker>();
        builder.Services.AddSingleton<IContextMenuPresenter, ContextMenuPresenter>();
        builder.Services.AddSingleton<IFileDialogService>(sp =>
            new FileDialogService(() =>
                Application.Current?.ApplicationLifetime
                    is IClassicDesktopStyleApplicationLifetime lt
                    ? lt.MainWindow as Visual
                    : null));
        builder.Services.AddSingleton<IKeyboardShortcutService, KeyboardShortcutService>();
        builder.Services.AddSingleton<IThemeService, ThemeService>();
        builder.Services.AddSingleton<IClipboardService>(sp =>
            new ClipboardService(() =>
                Application.Current?.ApplicationLifetime
                    is IClassicDesktopStyleApplicationLifetime lt2
                    ? TopLevel.GetTopLevel(lt2.MainWindow)
                    : null));
        builder.Services.AddSingleton<IDockManager, DockManager>();
        builder.Services.AddSingleton<IAvaloniaWindowManager, AvaloniaWindowManager>();

        // IWindowManager delegates to the same IAvaloniaWindowManager singleton
        builder.Services.AddSingleton<IWindowManager>(sp =>
            (IWindowManager)sp.GetRequiredService<IAvaloniaWindowManager>());

        // Persistence: debounce workspace saves triggered by WorkspaceSaveRequestedEvent
        builder.Services.AddHostedService<AvaloniaWorkspacePersistenceService>();

        var host = builder.Build();
        var settings = host.Services.GetRequiredService<DdsSettings>();

        if (settings.HeadlessMode != HeadlessMode.None)
        {
            // Record / Replay mode: run synchronously to block the main thread.
            host.Run();
        }
        else
        {
            // Force all diagnostic traces into a text file
            System.Diagnostics.Trace.Listeners.Add(new System.Diagnostics.TextWriterTraceListener("avalonia-startup-crash.log"));
            System.Diagnostics.Trace.AutoFlush = true;

            // 1. Build and initialize Avalonia platform subsystems FIRST (injects Win32)
            var appBuilder = BuildAvaloniaApp(host.Services)
                .LogToTrace(global::Avalonia.Logging.LogEventLevel.Verbose);

            // 2. NOW it is safe to start the Generic Host.
            // When AvaloniaWorkspacePersistenceService resolves AvaloniaWindowManager 
            // and touches the Window class, Avalonia is already fully hooked into the OS.
             _ = host.StartAsync();

            // Initialize plugins after host starts, before showing the window
            var pluginLoader = host.Services.GetRequiredService<PluginLoader>();
            var monitorContext = host.Services.GetRequiredService<IMonitorContext>();
            pluginLoader.InitializePlugins(monitorContext);

            // Use the already-built appBuilder — do NOT call BuildAvaloniaApp a second time
            appBuilder.StartWithClassicDesktopLifetime(args);
        }
    }

    private static AppBuilder BuildAvaloniaApp(IServiceProvider services) =>
        AppBuilder.Configure(() => new App(services))
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}

