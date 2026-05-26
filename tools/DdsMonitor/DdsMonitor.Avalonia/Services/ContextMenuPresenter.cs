using Avalonia;
using Avalonia.Controls;
using DdsMonitor.Avalonia.Core;
using DdsMonitor.Engine.Plugins;

namespace DdsMonitor.Avalonia.Services;

public sealed class ContextMenuPresenter : IContextMenuPresenter
{
    private readonly IContextMenuRegistry _registry;

    public ContextMenuPresenter(IContextMenuRegistry registry)
    {
        _registry = registry;
    }

    public void Show(Control anchor, object dataContext,
                     IReadOnlyList<ContextMenuItem>? defaultItems = null)
    {
        ArgumentNullException.ThrowIfNull(anchor);

        // Collect default items, then augment with registry items for the context type
        var items = new List<ContextMenuItem>();
        if (defaultItems is not null) items.AddRange(defaultItems);
        items.AddRange(GetRegistryItems(dataContext));

        var menu = new ContextMenu();
        foreach (var item in items)
        {
            var mi = new MenuItem { Header = item.Label };
            mi.Click += async (_, _) => await item.Action();
            menu.Items.Add(mi);
        }

        anchor.ContextMenu = menu;
        menu.Open(anchor);
    }

    private IEnumerable<ContextMenuItem> GetRegistryItems(object dataContext)
    {
        // Use reflection to invoke GetItems<TContext> with the runtime type
        var method = typeof(IContextMenuRegistry)
            .GetMethod(nameof(IContextMenuRegistry.GetItems))!
            .MakeGenericMethod(dataContext.GetType());
        return (IEnumerable<ContextMenuItem>)method.Invoke(_registry, new[] { dataContext })
               ?? Enumerable.Empty<ContextMenuItem>();
    }
}
