using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using DdsMonitor.Avalonia.Core;
using DdsMonitor.Avalonia.Services;
using DdsMonitor.Engine.Plugins;
using Xunit;

namespace DdsMonitor.Avalonia.Tests.Services;

public sealed class ContextMenuPresenterTests
{
    private sealed class StubContextMenuRegistry : IContextMenuRegistry
    {
        private readonly IReadOnlyList<ContextMenuItem> _items;

        public StubContextMenuRegistry(params ContextMenuItem[] items)
        {
            _items = items;
        }

        public void RegisterProvider<TContext>(
            Func<TContext, IEnumerable<ContextMenuItem>> provider) { }

        public IEnumerable<ContextMenuItem> GetItems<TContext>(TContext context) => _items;
    }

    [AvaloniaFact]
    public void Show_CombinesDefaultAndRegistryItems()
    {
        var registry = new StubContextMenuRegistry(
            new ContextMenuItem("Item A", null, () => Task.CompletedTask),
            new ContextMenuItem("Item B", null, () => Task.CompletedTask));
        var presenter = new ContextMenuPresenter(registry);

        var anchor   = new TextBlock();
        var defaults = new List<ContextMenuItem>
        {
            new ContextMenuItem("Default", null, () => Task.CompletedTask),
        };

        presenter.Show(anchor, new object(), defaults);

        Assert.NotNull(anchor.ContextMenu);
        Assert.Equal(3, anchor.ContextMenu!.Items.Count);
    }

    [AvaloniaFact]
    public void Show_NullAnchor_Throws()
    {
        var presenter = new ContextMenuPresenter(
            new StubContextMenuRegistry());
        Assert.Throws<ArgumentNullException>(
            () => presenter.Show(null!, new object()));
    }
}
