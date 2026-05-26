using System;
using System.Collections.Generic;
using Avalonia.Input;

namespace DdsMonitor.Avalonia.Core;

/// <summary>
/// A keyboard shortcut that has been registered with <see cref="IKeyboardShortcutService"/>.
/// </summary>
/// <param name="Gesture">The key gesture that triggers the shortcut.</param>
/// <param name="Description">Human-readable description of the shortcut.</param>
/// <param name="Action">The action invoked when the gesture is detected.</param>
public record RegisteredShortcut(KeyGesture Gesture, string Description, Action Action);

/// <summary>
/// Allows plugins to register and query global keyboard shortcuts.
/// </summary>
public interface IKeyboardShortcutService
{
    /// <summary>Registers a global keyboard shortcut.</summary>
    void Register(KeyGesture gesture, string description, Action action);

    /// <summary>Returns all currently registered shortcuts.</summary>
    IReadOnlyList<RegisteredShortcut> Registered { get; }

    /// <summary>Attempts to invoke the action associated with <paramref name="gesture"/>.</summary>
    /// <returns><c>true</c> if a matching shortcut was found and invoked.</returns>
    bool TryInvoke(KeyGesture gesture);
}
