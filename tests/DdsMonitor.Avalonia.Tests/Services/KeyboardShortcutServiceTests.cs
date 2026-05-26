using Avalonia.Input;
using DdsMonitor.Avalonia.Core;
using DdsMonitor.Avalonia.Services;
using Xunit;

namespace DdsMonitor.Avalonia.Tests.Services;

public sealed class KeyboardShortcutServiceTests
{
    [Fact]
    public void Register_ThenInvokeAction_RunsAction()
    {
        var svc = new KeyboardShortcutService();
        bool ran = false;
        svc.Register(new KeyGesture(Key.F1), "Test", () => ran = true);

        // Verify registered and invoke the action directly (TryInvoke uses Dispatcher.UIThread.Post)
        var shortcut = svc.Registered.First(s => s.Gesture.Key == Key.F1);
        shortcut.Action();

        Assert.True(ran);
    }

    [Fact]
    public void Registered_ReturnsAllRegisteredShortcuts()
    {
        var svc = new KeyboardShortcutService();
        svc.Register(new KeyGesture(Key.F2), "Shortcut A", () => { });
        svc.Register(new KeyGesture(Key.F3), "Shortcut B", () => { });

        Assert.Equal(2, svc.Registered.Count);
    }

    [Fact]
    public void Register_NullGesture_Throws()
    {
        var svc = new KeyboardShortcutService();
        Assert.Throws<ArgumentNullException>(() => svc.Register(null!, "x", () => { }));
    }

    [Fact]
    public void Register_NullAction_Throws()
    {
        var svc = new KeyboardShortcutService();
        Assert.Throws<ArgumentNullException>(
            () => svc.Register(new KeyGesture(Key.F4), "x", null!));
    }
}
