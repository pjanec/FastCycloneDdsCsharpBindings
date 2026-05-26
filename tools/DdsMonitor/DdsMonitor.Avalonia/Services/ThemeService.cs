using Avalonia;
using Avalonia.Styling;
using Avalonia.Threading;
using DdsMonitor.Avalonia.Core;

namespace DdsMonitor.Avalonia.Services;

public sealed class ThemeService : IThemeService
{
    private readonly IUserSettings _settings;
    private ThemeMode _currentMode;

    public ThemeService(IUserSettings settings)
    {
        _settings = settings;
        _currentMode = _settings.Get("Theme", "Mode", ThemeMode.System);
        Apply(_currentMode);
    }

    public ThemeMode CurrentMode => _currentMode;
    public event Action<ThemeMode>? ModeChanged;

    public void SetMode(ThemeMode mode)
    {
        if (_currentMode == mode) return;
        _currentMode = mode;
        _settings.Set("Theme", "Mode", mode);
        _ = _settings.SaveAsync();
        Apply(mode);
        ModeChanged?.Invoke(mode);
    }

    private static void Apply(ThemeMode mode)
    {
        if (Application.Current is null) return;
        if (!Dispatcher.UIThread.CheckAccess()) return;
        Application.Current.RequestedThemeVariant = mode switch
        {
            ThemeMode.Light => ThemeVariant.Light,
            ThemeMode.Dark  => ThemeVariant.Dark,
            _               => ThemeVariant.Default,
        };
    }
}
