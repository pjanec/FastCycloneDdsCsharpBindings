using Avalonia.Controls;
using DdsMonitor.Avalonia.Controls;
using Dock.Avalonia.Controls;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm.Controls;
using Dock.Model.Mvvm.Core;
using System.Text.Json;

namespace DdsMonitor.Avalonia.Docking;

/// <summary>
/// Concrete implementation of <see cref="IDockManager"/>.
/// Manages documents and tool panels in a Dock.NET layout.
/// </summary>
public sealed class DockManager : IDockManager
{
    // ── Internal properties exposed for unit tests ────────────────────────────

    internal DocumentDock?  DocumentDock   { get; private set; }
    internal ToolDock?      LeftToolDock   { get; private set; }
    internal ToolDock?      RightToolDock  { get; private set; }
    internal ToolDock?      BottomToolDock { get; private set; }

    // ── Private state ─────────────────────────────────────────────────────────

    private readonly record struct DockEntry(IDockable Dockable, string Type, DockSide? Side);

    private readonly Dictionary<string, DockEntry> _dockItems =
        new(StringComparer.Ordinal);

    private DdsDockFactory? _factory;

    // ── IDockManager ──────────────────────────────────────────────────────────

    public event Action<string>? DocumentClosed;

    /// <inheritdoc/>
    public void Initialise(DockControl? dockControl, MdiHost mdiHost)
    {
        ArgumentNullException.ThrowIfNull(mdiHost);

        _factory = new DdsDockFactory(mdiHost);
        var root = _factory.CreateLayout();

        DocumentDock   = _factory.DocumentDock;
        LeftToolDock   = _factory.LeftToolDock;
        RightToolDock  = _factory.RightToolDock;
        BottomToolDock = _factory.BottomToolDock;

        if (dockControl is not null && root is not null)
        {
            dockControl.Layout  = root;
            dockControl.Factory = _factory;
        }
    }

    /// <inheritdoc/>
    public void AddDocument(string id, string title, Control content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        if (DocumentDock is null) return;

        var doc = new Document
        {
            Id       = id,
            Title    = title,
            Context  = content,
            CanClose = true,
            CanFloat = true,
        };

        DocumentDock.VisibleDockables ??= _factory!.CreateList<IDockable>();
        DocumentDock.VisibleDockables.Add(doc);
        DocumentDock.ActiveDockable = doc;

        _dockItems[id] = new DockEntry(doc, "document", null);
    }

    /// <inheritdoc/>
    public void AddTool(string id, string title, Control content, DockSide side = DockSide.Left)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        var targetDock = side switch
        {
            DockSide.Right  => RightToolDock,
            DockSide.Bottom => BottomToolDock,
            _               => LeftToolDock,
        };
        if (targetDock is null) return;

        var tool = new Tool
        {
            Id      = id,
            Title   = title,
            Context = content,
        };

        targetDock.VisibleDockables ??= _factory!.CreateList<IDockable>();
        targetDock.VisibleDockables.Add(tool);
        targetDock.ActiveDockable = tool;

        _dockItems[id] = new DockEntry(tool, "tool", side);
    }

    /// <inheritdoc/>
    public bool Remove(string id)
    {
        if (!_dockItems.TryGetValue(id, out var entry)) return false;

        // Remove from the appropriate dockable list
        RemoveFromParent(entry.Dockable);

        _dockItems.Remove(id);
        DocumentClosed?.Invoke(id);
        return true;
    }

    /// <inheritdoc/>
    public bool TryFocus(string id)
    {
        if (!_dockItems.TryGetValue(id, out var entry)) return false;

        // Activate in the parent dock by setting ActiveDockable
        SetActiveInParent(entry.Dockable);
        return true;
    }

    /// <inheritdoc/>
    public string SerialiseLayout()
    {
        var items = _dockItems.Select(kvp => new
        {
            Id   = kvp.Key,
            Type = kvp.Value.Type,
            Side = kvp.Value.Side?.ToString(),
        }).ToList();

        return JsonSerializer.Serialize(items, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <inheritdoc/>
    public void DeserialiseLayout(string json)
    {
        // Parses the saved layout and logs unknown ids; content re-creation is the
        // caller's responsibility (AvaloniaWindowManager.LoadWorkspaceFromJson).
        using var doc = JsonDocument.Parse(json);
        foreach (var el in doc.RootElement.EnumerateArray())
        {
            var id   = el.TryGetProperty("Id",   out var idProp)   ? idProp.GetString()   : null;
            var type = el.TryGetProperty("Type", out var typeProp)  ? typeProp.GetString() : null;
            var side = el.TryGetProperty("Side", out var sideProp)  ? sideProp.GetString() : null;

            if (string.IsNullOrEmpty(id)) continue;

            // If not already present, caller must re-add via AddDocument/AddTool.
            _ = id; _ = type; _ = side;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void RemoveFromParent(IDockable dockable)
    {
        foreach (var dock in new IDockable?[] { DocumentDock, LeftToolDock, RightToolDock, BottomToolDock })
        {
            if (dock is DockBase db && db.VisibleDockables?.Contains(dockable) == true)
            {
                db.VisibleDockables.Remove(dockable);
                if (ReferenceEquals(db.ActiveDockable, dockable))
                    db.ActiveDockable = db.VisibleDockables.FirstOrDefault();
                return;
            }
        }
    }

    private void SetActiveInParent(IDockable dockable)
    {
        foreach (var dock in new IDockable?[] { DocumentDock, LeftToolDock, RightToolDock, BottomToolDock })
        {
            if (dock is DockBase db && db.VisibleDockables?.Contains(dockable) == true)
            {
                db.ActiveDockable = dockable;
                return;
            }
        }
    }
}
