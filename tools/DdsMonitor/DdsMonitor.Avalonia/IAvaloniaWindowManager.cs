using DdsMonitor.Avalonia.Controls;
using DdsMonitor.Avalonia.Core;
using DdsMonitor.Avalonia.Docking;
using DdsMonitor.Engine;

namespace DdsMonitor.Avalonia;

/// <summary>
/// Extends <see cref="IWindowManager"/> with Avalonia-specific operations:
/// connecting the MDI host and dock manager, spawning panels into a specific
/// layout target, and moving panels between layout modes.
/// </summary>
public interface IAvaloniaWindowManager : IWindowManager
{
    /// <summary>Provides the MDI host control that MDI-mode panels are added to.</summary>
    void SetMdiHost(MdiHost host);

    /// <summary>Provides the dock manager that Document/Tool-mode panels are routed to.</summary>
    void SetDockManager(IDockManager dockManager);

    /// <summary>
    /// Spawns a panel into the specified layout mode.
    /// If the panel is already open the existing instance is brought to front.
    /// </summary>
    PanelState SpawnPanel(string componentTypeName, LayoutKind layout,
        Dictionary<string, object>? initialState = null);

    /// <summary>
    /// Moves an already-open panel to a different layout mode.
    /// Not yet implemented; deferred to M2.
    /// </summary>
    void MoveToLayout(string panelId, LayoutKind newKind);
}
