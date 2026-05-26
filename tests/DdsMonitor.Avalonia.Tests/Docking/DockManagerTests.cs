using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using DdsMonitor.Avalonia.Controls;
using DdsMonitor.Avalonia.Docking;
using Xunit;

namespace DdsMonitor.Avalonia.Tests.Docking;

/// <summary>
/// Headless tests for <see cref="DockManager"/>.
/// Uses a null DockControl (no Dock.NET visual tree) so tests run in any headless context.
/// </summary>
public sealed class DockManagerTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (DockManager manager, MdiHost mdiHost) CreateManager()
    {
        var mdiHost = new MdiHost { Width = 800, Height = 600 };
        var manager = new DockManager();
        manager.Initialise(dockControl: null, mdiHost: mdiHost);
        return (manager, mdiHost);
    }

    // ── 1. AddDocument creates a document tab ─────────────────────────────────

    [AvaloniaFact]
    public void AddDocument_CreatesDocumentTab()
    {
        var (manager, _) = CreateManager();

        manager.AddDocument("doc1", "Document 1", new TextBlock { Text = "Hello" });

        // DocumentDock starts with __mdi_workspace, then doc1 → count = 2
        Assert.Equal(2, manager.DocumentDock!.VisibleDockables!.Count);
        Assert.Equal("doc1", manager.DocumentDock.ActiveDockable!.Id);
    }

    // ── 2. AddTool populates the left tool dock ───────────────────────────────

    [AvaloniaFact]
    public void AddTool_MakesLeftToolDockVisible()
    {
        var (manager, _) = CreateManager();

        manager.AddTool("tool1", "Tool 1", new TextBlock { Text = "Tool" }, DockSide.Left);

        Assert.Equal(1, manager.LeftToolDock!.VisibleDockables!.Count);
        Assert.Equal("tool1", manager.LeftToolDock.ActiveDockable!.Id);
    }

    // ── 3. Remove fires DocumentClosed event ─────────────────────────────────

    [AvaloniaFact]
    public void Remove_DocumentFiresDocumentClosed()
    {
        var (manager, _) = CreateManager();
        manager.AddDocument("doc1", "Doc 1", new TextBlock());

        string? closedId = null;
        manager.DocumentClosed += id => closedId = id;

        var removed = manager.Remove("doc1");

        Assert.True(removed);
        Assert.Equal("doc1", closedId);
        // After removal DocumentDock only has __mdi_workspace remaining
        Assert.Equal(1, manager.DocumentDock!.VisibleDockables!.Count);
    }

    // ── 4. SerialiseLayout round-trip contains expected ids ───────────────────

    [AvaloniaFact]
    public void SerialiseLayout_RoundTrip()
    {
        var (manager, _) = CreateManager();
        manager.AddDocument("doc1", "Doc", new TextBlock());
        manager.AddTool("tool1", "Tool", new TextBlock(), DockSide.Right);

        var json = manager.SerialiseLayout();

        Assert.Contains("doc1",  json);
        Assert.Contains("tool1", json);
        Assert.Contains("document", json);
        Assert.Contains("tool",     json);
    }
}
