using Avalonia.Controls;
using DdsMonitor.Avalonia.Controls;
using DdsMonitor.Avalonia.Core;
using DdsMonitor.Avalonia.Docking;
using DdsMonitor.Engine;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace DdsMonitor.Avalonia;

/// <summary>
/// Avalonia implementation of <see cref="IAvaloniaWindowManager"/>.
/// Routes panels to the MDI host (<see cref="LayoutKind.Mdi"/>) or to the dock manager
/// (<see cref="LayoutKind.DockDocument"/> / <see cref="LayoutKind.DockTool"/>)
/// depending on the requested layout.
/// </summary>
public sealed class AvaloniaWindowManager : IAvaloniaWindowManager
{
    private readonly IAvaloniaViewRegistry _viewRegistry;
    private readonly IServiceProvider _services;
    private readonly IEventBroker _eventBroker;

    private readonly object _lock = new();
    private readonly List<PanelState> _activePanels = new();
    private readonly Dictionary<string, object> _viewModels  = new(StringComparer.Ordinal);
    private readonly List<string> _excludedTopics = new();
    private readonly Dictionary<string, LayoutKind> _layoutKinds  = new(StringComparer.Ordinal);
    private readonly Dictionary<string, MdiChild>   _mdiChildren  = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Type>       _registeredPanelTypes = new(StringComparer.Ordinal);

    private MdiHost?      _mdiHost;
    private IDockManager? _dockManager;

    public AvaloniaWindowManager(
        IAvaloniaViewRegistry viewRegistry,
        IServiceProvider services,
        IEventBroker eventBroker)
    {
        _viewRegistry = viewRegistry;
        _services = services;
        _eventBroker = eventBroker;
    }

    // ── IWindowManager ────────────────────────────────────────────────────────

    public event Action<PanelState>? PanelClosed;
    public event Action? PanelsChanged;

    public IReadOnlyList<PanelState> ActivePanels
    {
        get { lock (_lock) return _activePanels.ToList(); }
    }

    public IReadOnlyList<string> ExcludedTopics
    {
        get { lock (_lock) return _excludedTopics.ToList(); }
    }

    public void SetExcludedTopics(IEnumerable<string> topicTypeNames)
    {
        ArgumentNullException.ThrowIfNull(topicTypeNames);
        lock (_lock)
        {
            _excludedTopics.Clear();
            _excludedTopics.AddRange(topicTypeNames);
        }
    }

    // ── IAvaloniaWindowManager ────────────────────────────────────────────────

    public void SetMdiHost(MdiHost host)
    {
        ArgumentNullException.ThrowIfNull(host);
        if (_mdiHost is not null)
            _mdiHost.ChildRemoved -= OnMdiChildRemoved;
        _mdiHost = host;
        _mdiHost.ChildRemoved += OnMdiChildRemoved;
    }

    private void OnMdiChildRemoved(object? sender, string childId) => OnPanelRemoved(childId);

    public void SetDockManager(IDockManager dockManager)
    {
        ArgumentNullException.ThrowIfNull(dockManager);
        if (_dockManager is not null)
            _dockManager.DocumentClosed -= OnPanelRemoved;
        _dockManager = dockManager;
        _dockManager.DocumentClosed += OnPanelRemoved;
    }

    /// <summary>Spawns a panel in the default MDI layout.</summary>
    public PanelState SpawnPanel(string componentTypeName, Dictionary<string, object>? initialState = null)
        => SpawnPanel(componentTypeName, LayoutKind.Mdi, initialState);

    /// <summary>
    /// Spawns a panel in the specified layout.
    /// If the panel is already open the existing instance is brought to front.
    /// </summary>
    public PanelState SpawnPanel(string componentTypeName, LayoutKind layout,
        Dictionary<string, object>? initialState = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(componentTypeName);

        var panelId = componentTypeName;

        lock (_lock)
        {
            if (_activePanels.Any(p => p.PanelId == panelId))
            {
                BringToFront(panelId);
                return _activePanels.First(p => p.PanelId == panelId);
            }
        }

        var panelState = new PanelState
        {
            PanelId           = panelId,
            Title             = componentTypeName,
            ComponentTypeName = componentTypeName,
            ComponentState    = initialState ?? new Dictionary<string, object>(StringComparer.Ordinal),
        };

        // Restore geometry from component state if available
        if (panelState.ComponentState.TryGetValue("__window", out var geo))
        {
            Dictionary<string, object>? geoDict = null;
            if (geo is Dictionary<string, object> nativeDict)
                geoDict = nativeDict;
            else if (geo is JsonElement je && je.ValueKind == JsonValueKind.Object)
            {
                geoDict = je.Deserialize<Dictionary<string, object>>() ?? new();
                panelState.ComponentState["__window"] = geoDict;
            }

            if (geoDict is not null)
            {
                if (geoDict.TryGetValue("X",      out var x)) panelState.X      = ToDouble(x);
                if (geoDict.TryGetValue("Y",      out var y)) panelState.Y      = ToDouble(y);
                if (geoDict.TryGetValue("Width",  out var w)) panelState.Width  = ToDouble(w);
                if (geoDict.TryGetValue("Height", out var h)) panelState.Height = ToDouble(h);
            }
        }

        // Build view content
        var (content, vm) = BuildContent(panelState);

        lock (_lock)
        {
            _activePanels.Add(panelState);
            _layoutKinds[panelId] = layout;
            if (vm is not null) _viewModels[panelId] = vm;
        }

        PanelsChanged?.Invoke();

        if (layout == LayoutKind.Mdi)
            SpawnMdiPanel(panelState, content);
        else
            SpawnDockPanel(panelState, content, layout);

        return panelState;
    }

    public void MoveToLayout(string panelId, LayoutKind newKind)
        => throw new NotSupportedException("MoveToLayout is deferred to M2.");

    // ── IWindowManager (continued) ────────────────────────────────────────────

    public void ClosePanel(string panelId)
    {
        LayoutKind layout;
        lock (_lock) _layoutKinds.TryGetValue(panelId, out layout);

        if (layout == LayoutKind.Mdi)
        {
            if (_mdiHost is not null)
            {
                // Remove returns false when the child isn't in MdiHost (e.g. no canvas yet)
                bool removed = _mdiHost.Remove(panelId);
                if (!removed) OnPanelRemoved(panelId);
            }
            else
                OnPanelRemoved(panelId);
        }
        else
        {
            _dockManager?.Remove(panelId);
        }
    }

    public void BringToFront(string panelId)
    {
        LayoutKind layout;
        lock (_lock) _layoutKinds.TryGetValue(panelId, out layout);

        if (layout == LayoutKind.Mdi)
            _mdiHost?.BringToFront(panelId);
        else
            _dockManager?.TryFocus(panelId);
    }

    public void ShowPanel(string panelId)
    {
        MdiChild? child;
        PanelState? state;
        lock (_lock)
        {
            _mdiChildren.TryGetValue(panelId, out child);
            state = _activePanels.FirstOrDefault(p => p.PanelId == panelId);
        }

        if (child is not null)
        {
            _mdiHost?.Restore(panelId);
            if (state is not null && state.IsHidden)
            {
                state.IsHidden = false;
                PanelsChanged?.Invoke();
            }
        }
        else
        {
            BringToFront(panelId);
        }
    }

    public void ClearPanels()
    {
        List<string> ids;
        lock (_lock) ids = _activePanels.Select(p => p.PanelId).ToList();
        foreach (var id in ids)
            ClosePanel(id);
    }

    // ── Panel type registry ───────────────────────────────────────────────────

    public IReadOnlyDictionary<string, Type> RegisteredPanelTypes
    {
        get { lock (_lock) return new Dictionary<string, Type>(_registeredPanelTypes, StringComparer.Ordinal); }
    }

    public void RegisterPanelType(string typeName, Type panelType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(typeName);
        ArgumentNullException.ThrowIfNull(panelType);
        lock (_lock) _registeredPanelTypes[typeName] = panelType;
    }

    // ── Workspace persistence ─────────────────────────────────────────────────

    public void SaveWorkspace(string filePath)
        => File.WriteAllText(filePath, SaveWorkspaceToJson());

    public string SaveWorkspaceToJson()
    {
        List<PanelState> panels;
        List<string> excludedSnapshot;
        Dictionary<string, LayoutKind> layoutSnapshot;

        lock (_lock)
        {
            panels           = _activePanels.ToList();
            excludedSnapshot = _excludedTopics.ToList();
            layoutSnapshot   = new Dictionary<string, LayoutKind>(_layoutKinds, StringComparer.Ordinal);
        }

        var pluginBag = new Dictionary<string, object>(StringComparer.Ordinal);
        _eventBroker.Publish(new WorkspaceSavingEvent(pluginBag));

        var serialisedPanels = panels.Select(p => new
        {
            p.PanelId,
            p.Title,
            p.ComponentTypeName,
            p.ComponentState,
            LayoutKind = layoutSnapshot.TryGetValue(p.PanelId, out var lk) ? lk.ToString() : "Mdi",
        }).ToList();

        var doc = new
        {
            Panels         = serialisedPanels,
            ExcludedTopics = excludedSnapshot.Count > 0 ? excludedSnapshot : null,
            PluginSettings = pluginBag.Count > 0 ? pluginBag : null,
            DockLayout     = _dockManager?.SerialiseLayout(),
        };

        return JsonSerializer.Serialize(doc, new JsonSerializerOptions
        {
            WriteIndented        = true,
            PropertyNamingPolicy = null,
        });
    }

    public void LoadWorkspaceFromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("ExcludedTopics", out var excludedEl))
            SetExcludedTopics(excludedEl.Deserialize<List<string>>() ?? new());

        if (root.TryGetProperty("Panels", out var panelsEl))
        {
            foreach (var el in panelsEl.EnumerateArray())
            {
                var typeName = el.TryGetProperty("ComponentTypeName", out var tn) ? tn.GetString() : null;
                if (string.IsNullOrEmpty(typeName)) continue;

                var stateDict = el.TryGetProperty("ComponentState", out var cs)
                    ? cs.Deserialize<Dictionary<string, object>>() ?? new()
                    : new Dictionary<string, object>(StringComparer.Ordinal);

                var layoutStr = el.TryGetProperty("LayoutKind", out var lk) ? lk.GetString() : "Mdi";
                var layout = Enum.TryParse<LayoutKind>(layoutStr, out var lkVal) ? lkVal : LayoutKind.Mdi;

                SpawnPanel(typeName, layout, stateDict);
            }
        }

        var pluginSettings = new Dictionary<string, object>(StringComparer.Ordinal);
        if (root.TryGetProperty("PluginSettings", out var settingsEl))
        {
            var d = settingsEl.Deserialize<Dictionary<string, object>>();
            if (d is not null)
                pluginSettings = new Dictionary<string, object>(d, StringComparer.Ordinal);
        }

        _eventBroker.Publish(new WorkspaceLoadedEvent(pluginSettings));
    }

    public void LoadWorkspace(string filePath)
    {
        if (!File.Exists(filePath)) return;
        LoadWorkspaceFromJson(File.ReadAllText(filePath));
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private (Control content, object? vm) BuildContent(PanelState panelState)
    {
        try
        {
            var vmType = Type.GetType(panelState.ComponentTypeName)
                ?? AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => { try { return a.GetType(panelState.ComponentTypeName); } catch { return null; } })
                    .FirstOrDefault(t => t is not null);

            if (vmType is not null)
            {
                var vm = _services.GetService(vmType)
                    ?? ActivatorUtilities.CreateInstance(_services, vmType);

                if (vm is IStatefulViewModel stateful)
                    stateful.Initialize(panelState.ComponentState);

                return (_viewRegistry.BuildView(vm), vm);
            }

            return (new TextBlock { Text = $"Unknown panel: {panelState.ComponentTypeName}" }, null);
        }
        catch (Exception ex)
        {
            return (new TextBlock { Text = $"Error loading panel: {ex.Message}" }, null);
        }
    }

    private void SpawnMdiPanel(PanelState panelState, Control content)
    {
        if (_mdiHost is null) return;

        var child = new MdiChild
        {
            ChildId = panelState.PanelId,
            Title   = panelState.Title,
            Content = content,
            Width   = panelState.Width  > 0 ? panelState.Width  : 600,
            Height  = panelState.Height > 0 ? panelState.Height : 400,
        };

        // CloseRequested is handled via MdiHost.ChildRemoved (subscribed in SetMdiHost).
        child.MinimiseRequested += (_, _) => _mdiHost.Minimise(panelState.PanelId);

        lock (_lock) _mdiChildren[panelState.PanelId] = child;

        _mdiHost.Add(child,
            panelState.X      != 0 ? panelState.X      : 20,
            panelState.Y      != 0 ? panelState.Y      : 20,
            panelState.Width  >  0 ? panelState.Width  : 600,
            panelState.Height >  0 ? panelState.Height : 400);
    }

    private void SpawnDockPanel(PanelState panelState, Control content, LayoutKind layout)
    {
        if (_dockManager is null) return;

        if (layout == LayoutKind.DockTool)
            _dockManager.AddTool(panelState.PanelId, panelState.Title, content);
        else
            _dockManager.AddDocument(panelState.PanelId, panelState.Title, content);
    }

    private void OnPanelRemoved(string panelId)
    {
        PanelState? state;
        object? vm;
        MdiChild? child;
        lock (_lock)
        {
            state = _activePanels.FirstOrDefault(p => p.PanelId == panelId);
            _activePanels.RemoveAll(p => p.PanelId == panelId);
            _viewModels.TryGetValue(panelId, out vm);
            _viewModels.Remove(panelId);
            _layoutKinds.Remove(panelId);
            _mdiChildren.TryGetValue(panelId, out child);
            _mdiChildren.Remove(panelId);
        }

        if (vm is IDisposable disposable)
            disposable.Dispose();

        if (state is not null)
        {
            // Persist geometry into ComponentState so callers can restore position on re-spawn.
            // Use child dimensions if available (child was positioned by MdiHost), else fall back to state values.
            double x = state.X, y = state.Y, w = state.Width, h = state.Height;
            if (child is not null)
            {
                if (child.Width  > 0) w = child.Width;
                if (child.Height > 0) h = child.Height;
            }
            state.ComponentState["__window"] = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["X"] = x, ["Y"] = y, ["Width"] = w, ["Height"] = h,
            };

            PanelClosed?.Invoke(state);
        }

        PanelsChanged?.Invoke();
        _eventBroker.Publish(new WorkspaceSaveRequestedEvent());
    }

    private static double ToDouble(object value) =>
        value is System.Text.Json.JsonElement je ? je.GetDouble() : Convert.ToDouble(value);
}
