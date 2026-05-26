using Avalonia.Input;
using Avalonia.Threading;
using DdsMonitor.Avalonia.Core;

namespace DdsMonitor.Avalonia.Services;

public sealed class KeyboardShortcutService : IKeyboardShortcutService
{
    private readonly List<RegisteredShortcut> _shortcuts = new();
    private readonly object _lock = new();

    public IReadOnlyList<RegisteredShortcut> Registered
    {
        get { lock (_lock) return _shortcuts.ToList(); }
    }

    public void Register(KeyGesture gesture, string description, Action action)
    {
        ArgumentNullException.ThrowIfNull(gesture);
        ArgumentNullException.ThrowIfNull(action);
        lock (_lock) _shortcuts.Add(new RegisteredShortcut(gesture, description, action));
    }

    public bool TryInvoke(KeyGesture gesture)
    {
        RegisteredShortcut? match;
        lock (_lock) match = _shortcuts.FirstOrDefault(s => s.Gesture.Equals(gesture));
        if (match is null) return false;
        Dispatcher.UIThread.Post(match.Action);
        return true;
    }
}
