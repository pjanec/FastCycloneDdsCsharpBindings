using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using DdsMonitor.Avalonia.Controls;
using DdsMonitor.Avalonia.Core;
using DdsMonitor.Avalonia.Docking;
using DdsMonitor.Avalonia.FeatureDemoPlugin;
using DdsMonitor.Engine;
using DdsMonitor.Engine.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DdsMonitor.Avalonia.Tests.Smoke;

/// <summary>
/// M1 milestone smoke tests — fast high-level checks for the major deliverables.
/// </summary>
public sealed class M1SmokeTest
{
    // ─────────────────────────────────────────────────────────────────────────
    // Smoke Test 1: Cold start produces an empty shell
    // ─────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void M1_ColdStart_ProducesEmptyShell()
    {
        var menu    = new StubMenuRegistry();
        var toolbar = new ToolbarRegistry();
        var bridge  = new StubDdsBridge();
        var viewReg = new AvaloniaViewRegistry();
        var broker  = new StubEventBroker();

        var services = new ServiceCollection()
            .AddSingleton<IMenuRegistry>(menu)
            .AddSingleton<IToolbarRegistry>(toolbar)
            .AddSingleton<IDdsBridge>(bridge)
            .AddSingleton<IAvaloniaViewRegistry>(viewReg)
            .AddSingleton<IEventBroker>(broker)
            .AddSingleton<IAvaloniaWindowManager>(sp =>
                new AvaloniaWindowManager(viewReg, sp, broker))
            .AddSingleton<IWindowManager>(sp =>
                (IWindowManager)sp.GetRequiredService<IAvaloniaWindowManager>())
            .BuildServiceProvider();

        var window = new ShellWindow(services);

        // 4 fixed top-level items: File, View, Devel, Plugins
        var menuControl = window.FindControl<Menu>("MainMenu");
        Assert.NotNull(menuControl);
        Assert.Equal(4, menuControl!.Items.Count);

        // No panels spawned on cold start.
        var wm = services.GetRequiredService<IWindowManager>();
        Assert.Empty(wm.ActivePanels);

        Assert.Equal("DDS Monitor", window.Title);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Smoke Test 2: FeatureDemo plugin registers exactly five topic types
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task M1_FeatureDemo_RegistersFiveTopicTypes()
    {
        var registry = new CapturingTopicRegistry();
        var bridge   = new NoOpDdsBridge();
        var config   = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeatureDemoPlugin:Enabled"] = "false",
            })
            .Build();

        var svc = new DemoPublisherService(registry, bridge, config);
        await svc.StartAsync(CancellationToken.None);
        await svc.StopAsync(CancellationToken.None);

        var distinctTypes = new HashSet<Type>(
            registry.Registrations.ConvertAll(m => m.TopicType));

        Assert.Equal(5, distinctTypes.Count);
        Assert.Contains(typeof(TelemetrySample), distinctTypes);
        Assert.Contains(typeof(EntityState),     distinctTypes);
        Assert.Contains(typeof(AlertEvent),      distinctTypes);
        Assert.Contains(typeof(GeoLocation),     distinctTypes);
        Assert.Contains(typeof(UnionPayload),    distinctTypes);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Smoke Test 3: Blazor-format JSON (no LayoutKind field) loads as MDI
    // ─────────────────────────────────────────────────────────────────────────

    [AvaloniaFact]
    public void M1_WorkspaceRoundtrip_BlazorJsonLoadsAsMdi()
    {
        var viewRegistry  = new AvaloniaViewRegistry();
        var eventBroker   = new StubEventBroker();
        var dockManager   = new SmokeStubDockManager();
        var serviceProvider = new SmokeStubServiceProvider();

        // Register a view factory for "BlazorPanel" (any unknown name gets a TextBlock fallback
        // inside AvaloniaWindowManager, so no explicit registration is needed).

        var manager = new AvaloniaWindowManager(viewRegistry, serviceProvider, eventBroker);
        var mdiHost = new MdiHost { Width = 800, Height = 600 };
        manager.SetMdiHost(mdiHost);
        manager.SetDockManager(dockManager);

        // "Blazor-format" workspace JSON — no LayoutKind field, so defaults to Mdi.
        const string blazorJson = """
            {
              "Panels": [
                {
                  "ComponentTypeName": "BlazorUnknownPanel",
                  "ComponentState": {}
                }
              ]
            }
            """;

        manager.LoadWorkspaceFromJson(blazorJson);

        // One panel spawned.
        Assert.Single(manager.ActivePanels);

        // MDI host contains one child (unknown type gets placeholder TextBlock content).
        Assert.Equal(1, mdiHost.Children.Count);

        // Saved JSON must contain LayoutKind serialised as "Mdi".
        var saved = manager.SaveWorkspaceToJson();
        Assert.Contains("LayoutKind", saved);
        Assert.Contains("Mdi",        saved);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Inline stubs
    // ─────────────────────────────────────────────────────────────────────────

    private sealed class CapturingTopicRegistry : ITopicRegistry
    {
        public List<TopicMetadata> Registrations { get; } = [];
        public event Action? Changed;
        public IReadOnlyList<TopicMetadata> AllTopics => Registrations;
        public TopicMetadata? GetByType(Type t) => Registrations.Find(m => m.TopicType == t);
        public TopicMetadata? GetByName(string n) => Registrations.Find(m => m.TopicName == n);
        public void Register(TopicMetadata meta) { Registrations.Add(meta); Changed?.Invoke(); }
    }

    private sealed class NoOpDdsBridge : IDdsBridge
    {
        public CycloneDDS.Runtime.DdsParticipant Participant => null!;
        public IReadOnlyList<CycloneDDS.Runtime.DdsParticipant> Participants => [];
        public IReadOnlyList<ParticipantConfig> ParticipantConfigs => [];
        public string? CurrentPartition => null;
        public bool IsPaused { get; set; }
        public IReadOnlyDictionary<Type, IDynamicReader> ActiveReaders => new Dictionary<Type, IDynamicReader>();
        public IReadOnlySet<Type> ExplicitlyUnsubscribedTopicTypes => new HashSet<Type>();
        public event Action? ReadersChanged { add { } remove { } }

        public IDynamicWriter GetWriter(TopicMetadata meta) => new NullWriter(meta.TopicType);
        public IDynamicReader Subscribe(TopicMetadata meta) => null!;
        public bool TrySubscribe(TopicMetadata meta, out IDynamicReader? r, out string? e)
        { r = null; e = null; return false; }
        public void Unsubscribe(TopicMetadata meta) { }
        public void ChangePartition(string? p) { }
        public void InitializeExplicitlyUnsubscribed(IEnumerable<Type> t) { }
        public void AddParticipant(uint d, string p) { }
        public void RemoveParticipant(int i) { }
        public void ResetAll() { }
        public void Dispose() { }

        private sealed class NullWriter : IDynamicWriter
        {
            public NullWriter(Type t) => TopicType = t;
            public Type TopicType { get; }
            public void Write(object payload) { }
            public void DisposeInstance(object payload) { }
            public void Dispose() { }
        }
    }

    private sealed class SmokeStubDockManager : IDockManager
    {
        public event Action<string>? DocumentClosed;
        public void Initialise(Dock.Avalonia.Controls.DockControl? d, MdiHost h) { }
        public void AddDocument(string id, string t, Control c) { }
        public void AddTool(string id, string t, Control c, DockSide s = DockSide.Left) { }
        public bool Remove(string id) { DocumentClosed?.Invoke(id); return true; }
        public bool TryFocus(string id) => false;
        public string SerialiseLayout() => "{}";
        public void DeserialiseLayout(string json) { }
    }

    private sealed class SmokeStubServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }
}
