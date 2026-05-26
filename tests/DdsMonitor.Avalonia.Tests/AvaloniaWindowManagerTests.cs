using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using DdsMonitor.Avalonia.Controls;
using DdsMonitor.Avalonia.Core;
using DdsMonitor.Avalonia.Docking;
using DdsMonitor.Engine;
using Dock.Avalonia.Controls;
using Xunit;

namespace DdsMonitor.Avalonia.Tests;

/// <summary>
/// Headless tests for <see cref="AvaloniaWindowManager"/>.
/// </summary>
public sealed class AvaloniaWindowManagerDockTests
{
    // ── Stub implementations ──────────────────────────────────────────────────

    private sealed class StubViewRegistry : IAvaloniaViewRegistry
    {
        public void Register<TViewModel>(Func<TViewModel, Control> viewFactory) { }
        public Control BuildView(object viewModel) => new TextBlock { Text = viewModel?.ToString() };
    }

    private sealed class StubServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    private sealed class StubEventBroker : IEventBroker
    {
        public void Publish<TEvent>(TEvent eventData) { }
        public IDisposable Subscribe<TEvent>(Action<TEvent> handler) => NullDisposable.Instance;

        private sealed class NullDisposable : IDisposable
        {
            public static readonly NullDisposable Instance = new();
            public void Dispose() { }
        }
    }

    private sealed class StubDockManager : IDockManager
    {
        public List<string> AddedDocuments { get; } = new();
        public List<string> AddedTools     { get; } = new();

        public event Action<string>? DocumentClosed;

        public void Initialise(DockControl? dockControl, MdiHost mdiHost) { }
        public void AddDocument(string id, string title, Control content) => AddedDocuments.Add(id);
        public void AddTool(string id, string title, Control content, DockSide side = DockSide.Left) => AddedTools.Add(id);
        public bool Remove(string id) { DocumentClosed?.Invoke(id); return true; }
        public bool TryFocus(string id) => true;
        public string SerialiseLayout() => "[]";
        public void DeserialiseLayout(string json) { }
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static (AvaloniaWindowManager manager, MdiHost mdiHost) CreateManager()
    {
        var mdiHost = new MdiHost { Width = 800, Height = 600 };
        var manager = new AvaloniaWindowManager(
            new StubViewRegistry(),
            new StubServiceProvider(),
            new StubEventBroker());
        manager.SetMdiHost(mdiHost);
        return (manager, mdiHost);
    }

    // ── 1. SpawnPanel (MDI) adds child to MdiHost ─────────────────────────────

    [AvaloniaFact]
    public void SpawnPanel_Mdi_AddsChildToMdiHost()
    {
        var (manager, mdiHost) = CreateManager();

        manager.SpawnPanel("Unknown");

        Assert.Equal(1, mdiHost.Children.Count);
    }

    // ── 2. SpawnPanel (DockDocument) delegates to DockManager ─────────────────

    [AvaloniaFact]
    public void SpawnPanel_DockDocument_AddsToDocumentManager()
    {
        var (manager, mdiHost) = CreateManager();
        var stub = new StubDockManager();
        manager.SetDockManager(stub);

        manager.SpawnPanel("Unknown", LayoutKind.DockDocument);

        Assert.Contains("Unknown", stub.AddedDocuments);
    }

    // ── 3. ClosePanel removes from ActivePanels ───────────────────────────────

    [AvaloniaFact]
    public void ClosePanel_Mdi_RemovesFromActivePanels()
    {
        var (manager, mdiHost) = CreateManager();

        manager.SpawnPanel("MyPanel");
        Assert.Equal(1, manager.ActivePanels.Count);

        manager.ClosePanel("MyPanel");

        Assert.Equal(0, manager.ActivePanels.Count);
    }

    // ── 4. ExcludedTopics round-trip ──────────────────────────────────────────

    [AvaloniaFact]
    public void ExcludedTopics_SetAndGet()
    {
        var (manager, _) = CreateManager();

        manager.SetExcludedTopics(new[] { "a", "b" });

        Assert.Contains("a", manager.ExcludedTopics);
        Assert.Contains("b", manager.ExcludedTopics);
    }

    // ── 5. SaveWorkspaceToJson includes the spawned panel id ──────────────────

    [AvaloniaFact]
    public void SaveWorkspaceToJson_ContainsPanel()
    {
        var (manager, _) = CreateManager();

        manager.SpawnPanel("com.example.MyPanel");

        var json = manager.SaveWorkspaceToJson();

        Assert.Contains("com.example.MyPanel", json);
    }

    // ── 6. BringToFront for MDI calls through without exception ───────────────

    [AvaloniaFact]
    public void BringToFront_Mdi_DoesNotThrow()
    {
        var (manager, mdiHost) = CreateManager();

        // Create a window so MdiHost template is applied and children are visible
        var window = new Window { Content = mdiHost, Width = 800, Height = 600 };
        window.Show();
        try
        {
            manager.SpawnPanel("Panel1");
            manager.SpawnPanel("Panel2");

            var ex = Record.Exception(() => manager.BringToFront("Panel1"));
            Assert.Null(ex);
        }
        finally
        {
            window.Close();
        }
    }

    // ── Stub: records all published events ────────────────────────────────────

    private sealed class RecordingEventBroker : IEventBroker
    {
        private readonly List<object> _events;
        public RecordingEventBroker(List<object> events) => _events = events;

        public void Publish<TEvent>(TEvent eventData)
        {
            if (eventData is not null) _events.Add(eventData!);
        }

        public IDisposable Subscribe<TEvent>(Action<TEvent> handler) => NullDisposable.Instance;

        private sealed class NullDisposable : IDisposable
        {
            public static readonly NullDisposable Instance = new();
            public void Dispose() { }
        }
    }

    // ── 7. LoadWorkspaceFromJson: Blazor-format JSON defaults to MDI ──────────

    [AvaloniaFact]
    public void LoadWorkspaceFromJson_BlazorFormat_AllMdi()
    {
        var (manager, mdiHost) = CreateManager();
        var window = new Window { Content = mdiHost, Width = 800, Height = 600 };
        window.Show();
        try
        {
            // Blazor-format JSON — no LayoutKind, no DockLayout
            var json = """
                {
                    "Panels": [
                        { "PanelId": "p1", "Title": "P1", "ComponentTypeName": "Unknown1", "ComponentState": {} },
                        { "PanelId": "p2", "Title": "P2", "ComponentTypeName": "Unknown2", "ComponentState": {} }
                    ],
                    "ExcludedTopics": []
                }
                """;

            manager.LoadWorkspaceFromJson(json);

            // Both panels should be spawned as MDI (default)
            Assert.Equal(2, manager.ActivePanels.Count);
            Assert.Equal(2, mdiHost.Children.Count);
        }
        finally { window.Close(); }
    }

    // ── 8. ClosePanel publishes WorkspaceSaveRequestedEvent ───────────────────

    [AvaloniaFact]
    public void ClosePanel_PublishesWorkspaceSaveRequestedEvent()
    {
        var published = new List<object>();
        var broker = new RecordingEventBroker(published);
        var mdiHost = new MdiHost { Width = 800, Height = 600 };
        var manager = new AvaloniaWindowManager(new StubViewRegistry(), new StubServiceProvider(), broker);
        manager.SetMdiHost(mdiHost);

        var window = new Window { Content = mdiHost, Width = 800, Height = 600 };
        window.Show();
        try
        {
            manager.SpawnPanel("Panel1");
            Assert.Equal(1, manager.ActivePanels.Count);

            manager.ClosePanel("Panel1");

            Assert.Contains(published, e => e is WorkspaceSaveRequestedEvent);
        }
        finally { window.Close(); }
    }

    // ── 9. SaveWorkspaceToJson / LoadWorkspaceFromJson round-trips LayoutKind ─

    [AvaloniaFact]
    public void SaveAndLoad_RoundTrips_LayoutKind()
    {
        var (manager, mdiHost) = CreateManager();
        var stub = new StubDockManager();
        manager.SetDockManager(stub);

        var window = new Window { Content = mdiHost, Width = 800, Height = 600 };
        window.Show();
        try
        {
            manager.SpawnPanel("PanelA", LayoutKind.Mdi);
            manager.SpawnPanel("PanelB", LayoutKind.DockDocument);

            var json = manager.SaveWorkspaceToJson();

            Assert.Contains("\"LayoutKind\"", json);
            Assert.Contains("\"Mdi\"", json);
            Assert.Contains("\"DockDocument\"", json);
        }
        finally { window.Close(); }
    }
}
