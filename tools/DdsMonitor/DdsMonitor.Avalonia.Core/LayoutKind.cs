namespace DdsMonitor.Avalonia.Core;

/// <summary>
/// Describes how a spawned panel should be laid out inside the shell.
/// </summary>
public enum LayoutKind
{
    /// <summary>Free-floating MDI window.</summary>
    Mdi,

    /// <summary>Docked as a document tab.</summary>
    DockDocument,

    /// <summary>Docked as a tool panel.</summary>
    DockTool,
}
