using System;

namespace DdsMonitor.Avalonia.Core;

/// <summary>
/// The application colour / brightness mode.
/// </summary>
public enum ThemeMode
{
    /// <summary>Follow the OS setting.</summary>
    System,

    /// <summary>Always light.</summary>
    Light,

    /// <summary>Always dark.</summary>
    Dark,
}

/// <summary>
/// Allows the shell and plugins to query and change the application theme.
/// </summary>
public interface IThemeService
{
    /// <summary>Gets the currently active theme mode.</summary>
    ThemeMode CurrentMode { get; }

    /// <summary>Raised when the theme mode changes.</summary>
    event Action<ThemeMode>? ModeChanged;

    /// <summary>Sets the active theme mode.</summary>
    void SetMode(ThemeMode mode);
}
