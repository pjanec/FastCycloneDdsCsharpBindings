using Avalonia.Controls;
using Dock.Model.Controls;
using Dock.Model.Core;
using Dock.Model.Mvvm;
using Dock.Model.Mvvm.Controls;

namespace DdsMonitor.Avalonia.Docking;

/// <summary>
/// Builds the default Dock.NET layout:
/// a vertical ProportionalDock containing a horizontal inner dock
/// (left tools | documents | right tools) with a bottom tool dock below.
/// The MDI host control is embedded as a permanent, non-closeable document.
/// </summary>
internal sealed class DdsDockFactory : Factory
{
    private readonly Control _mdiHostContent;

    internal DocumentDock? DocumentDock  { get; private set; }
    internal ToolDock?     LeftToolDock  { get; private set; }
    internal ToolDock?     RightToolDock { get; private set; }
    internal ToolDock?     BottomToolDock { get; private set; }

    internal DdsDockFactory(Control mdiHostContent)
    {
        _mdiHostContent = mdiHostContent;
    }

    public override IRootDock? CreateLayout()
    {
        var mdiWorkspace = new Document
        {
            Id       = "__mdi_workspace",
            Title    = "MDI Workspace",
            Context  = _mdiHostContent,
            CanClose = false,
            CanFloat = false,
        };

        DocumentDock = new DocumentDock
        {
            Id               = "DocumentDock",
            Title            = "Documents",
            VisibleDockables = CreateList<IDockable>(mdiWorkspace),
            ActiveDockable   = mdiWorkspace,
            DefaultDockable  = mdiWorkspace,
        };

        LeftToolDock = new ToolDock
        {
            Id               = "LeftToolDock",
            Title            = "Left",
            Alignment        = Alignment.Left,
            VisibleDockables = CreateList<IDockable>(),
        };

        RightToolDock = new ToolDock
        {
            Id               = "RightToolDock",
            Title            = "Right",
            Alignment        = Alignment.Right,
            VisibleDockables = CreateList<IDockable>(),
        };

        BottomToolDock = new ToolDock
        {
            Id               = "BottomToolDock",
            Title            = "Bottom",
            Alignment        = Alignment.Bottom,
            VisibleDockables = CreateList<IDockable>(),
        };

        var innerLayout = new ProportionalDock
        {
            Id               = "InnerLayout",
            Orientation      = Orientation.Horizontal,
            VisibleDockables = CreateList<IDockable>(
                LeftToolDock,
                new ProportionalDockSplitter { Id = "LeftSplitter" },
                DocumentDock,
                new ProportionalDockSplitter { Id = "RightSplitter" },
                RightToolDock),
        };

        var outerLayout = new ProportionalDock
        {
            Id               = "OuterLayout",
            Orientation      = Orientation.Vertical,
            VisibleDockables = CreateList<IDockable>(
                innerLayout,
                new ProportionalDockSplitter { Id = "BottomSplitter" },
                BottomToolDock),
        };

        var root = CreateRootDock();
        root.Id               = "Root";
        root.VisibleDockables = CreateList<IDockable>(outerLayout);
        root.ActiveDockable   = outerLayout;
        root.DefaultDockable  = outerLayout;

        InitLayout(root);
        return root;
    }
}
