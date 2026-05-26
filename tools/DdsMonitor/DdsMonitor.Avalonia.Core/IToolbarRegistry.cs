using System;
using System.Collections.Generic;

namespace DdsMonitor.Avalonia.Core;

/// <summary>
/// Registry for toolbar entries. Plugins and the shell register toolbar buttons here.
/// </summary>
public interface IToolbarRegistry
{
    /// <summary>Gets the registered toolbar entries in registration order.</summary>
    IReadOnlyList<ToolbarEntry> Entries { get; }

    /// <summary>Raised whenever a new entry is registered.</summary>
    event Action? Changed;

    /// <summary>Registers a toolbar button.</summary>
    /// <param name="id">Unique identifier for the entry (used for deduplication / replacement).</param>
    /// <param name="onClick">Action invoked when the button is clicked.</param>
    /// <param name="label">Display label shown on the button (may be empty).</param>
    /// <param name="tooltip">Tooltip text shown on hover.</param>
    /// <param name="iconKey">Optional icon resource key.</param>
    void Register(string id, Action onClick, string label = "", string tooltip = "", string? iconKey = null);
}
