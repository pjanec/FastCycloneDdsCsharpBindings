using Avalonia.Controls;
using DdsMonitor.Avalonia.Controls;
using Dock.Avalonia.Controls;

namespace DdsMonitor.Avalonia.Docking;

/// <summary>Identifies which side of the document area a tool panel is docked to.</summary>
public enum DockSide { Left, Right, Bottom }

/// <summary>
/// Manages the Dock.NET layout: creates and removes document tabs and tool panels,
/// and serialises/deserialises the layout for workspace persistence.
/// </summary>
public interface IDockManager
{
    /// <summary>Connects the manager to the live Dock.NET control and the MDI host control.</summary>
    void Initialise(DockControl? dockControl, MdiHost mdiHost);

    /// <summary>Adds a closeable document tab to the document area.</summary>
    void AddDocument(string id, string title, Control content);

    /// <summary>Adds a tool panel to the specified side.</summary>
    void AddTool(string id, string title, Control content, DockSide side = DockSide.Left);

    /// <summary>Removes a dockable by id and fires <see cref="DocumentClosed"/>. Returns true if found.</summary>
    bool Remove(string id);

    /// <summary>Activates the dockable with the given id. Returns true if found.</summary>
    bool TryFocus(string id);

    /// <summary>Serialises the current layout to a JSON string.</summary>
    string SerialiseLayout();

    /// <summary>Restores a previously serialised layout.</summary>
    void DeserialiseLayout(string json);

    /// <summary>Raised when a dockable is removed via <see cref="Remove"/>.</summary>
    event Action<string>? DocumentClosed;
}
