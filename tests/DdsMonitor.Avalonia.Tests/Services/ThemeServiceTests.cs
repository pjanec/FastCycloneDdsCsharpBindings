using Avalonia.Styling;
using DdsMonitor.Avalonia.Core;
using DdsMonitor.Avalonia.Services;
using Avalonia.Headless.XUnit;
using Xunit;

namespace DdsMonitor.Avalonia.Tests.Services;

public sealed class ThemeServiceTests
{
    private sealed class StubUserSettings : IUserSettings
    {
        public T Get<T>(string section, string key, T defaultValue) => defaultValue;
        public void Set<T>(string section, string key, T value) { }
        public Task SaveAsync() => Task.CompletedTask;
    }

    [AvaloniaFact]
    public void SetMode_Dark_SetsThemeVariantDark()
    {
        var svc = new ThemeService(new StubUserSettings());
        ThemeMode? fired = null;
        svc.ModeChanged += m => fired = m;

        svc.SetMode(ThemeMode.Dark);

        Assert.Equal(ThemeMode.Dark, svc.CurrentMode);
        Assert.Equal(ThemeMode.Dark, fired);
        Assert.Equal(ThemeVariant.Dark,
            global::Avalonia.Application.Current?.RequestedThemeVariant);
    }

    [AvaloniaFact]
    public void SetMode_SameMode_DoesNotFireEvent()
    {
        var svc = new ThemeService(new StubUserSettings());
        int count = 0;
        svc.ModeChanged += _ => count++;

        svc.SetMode(ThemeMode.System); // already System (default)

        Assert.Equal(0, count);
    }

    [AvaloniaFact]
    public void SetMode_Light_SetsThemeVariantLight()
    {
        var svc = new ThemeService(new StubUserSettings());

        svc.SetMode(ThemeMode.Light);

        Assert.Equal(ThemeMode.Light, svc.CurrentMode);
        Assert.Equal(ThemeVariant.Light,
            global::Avalonia.Application.Current?.RequestedThemeVariant);
    }
}
