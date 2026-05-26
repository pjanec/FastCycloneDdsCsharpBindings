using System.Collections.Generic;
using Avalonia.Controls;
using DdsMonitor.Engine.Plugins;

namespace DdsMonitor.Avalonia.Core;

/// <summary>
/// Shows a context menu anchored to an Avalonia <see cref="Control"/>.
/// </summary>
public interface IContextMenuPresenter
{
    /// <summary>
    /// Displays a context menu anchored to <paramref name="anchor"/>.
    /// </summary>
    /// <param name="anchor">The control the menu is anchored to.</param>
    /// <param name="dataContext">Data context for the menu items.</param>
    /// <param name="defaultItems">Optional default items prepended to the menu.</param>
    void Show(Control anchor, object dataContext,
              IReadOnlyList<ContextMenuItem>? defaultItems = null);
}
