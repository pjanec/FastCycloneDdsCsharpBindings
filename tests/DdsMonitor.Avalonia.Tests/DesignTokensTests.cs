using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Headless.XUnit;
using Xunit;

namespace DdsMonitor.Avalonia.Tests;

/// <summary>
/// Verifies that all colour design tokens from DESIGN.md §7 are present and correct
/// in both Light and Dark theme variants.
/// </summary>
public sealed class DesignTokensTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Color LookupColor(string key, ThemeVariant variant)
    {
        var app = Application.Current
            ?? throw new InvalidOperationException("Application.Current is null — headless app not started.");
        app.TryGetResource(key, variant, out var resource);
        var brush = resource as ISolidColorBrush
            ?? throw new InvalidOperationException($"Resource '{key}' not found or is not a SolidColorBrush for variant {variant}.");
        return brush.Color;
    }

    // ── Light-theme token tests ───────────────────────────────────────────────

    [AvaloniaFact]
    public void DesignTokens_LightTheme_SurfaceBackground_IsCorrectColour()
    {
        var color = LookupColor("Surface.Background", ThemeVariant.Light);
        Assert.Equal(Color.Parse("#FFFAFAFA"), color); // alpha=FF is added by Avalonia
    }

    [AvaloniaFact]
    public void DesignTokens_DarkTheme_SurfaceBackground_IsCorrectColour()
    {
        var color = LookupColor("Surface.Background", ThemeVariant.Dark);
        Assert.Equal(Color.Parse("#FF1A1A1A"), color);
    }

    [AvaloniaFact]
    public void DesignTokens_LightTheme_SurfacePanel_IsCorrectColour()
    {
        var color = LookupColor("Surface.Panel", ThemeVariant.Light);
        Assert.Equal(Color.Parse("#FFFFFFFF"), color);
    }

    [AvaloniaFact]
    public void DesignTokens_DarkTheme_SurfacePanel_IsCorrectColour()
    {
        var color = LookupColor("Surface.Panel", ThemeVariant.Dark);
        Assert.Equal(Color.Parse("#FF242424"), color);
    }

    [AvaloniaFact]
    public void DesignTokens_LightTheme_BorderAccent_IsCorrectColour()
    {
        var color = LookupColor("Border.Accent", ThemeVariant.Light);
        Assert.Equal(Color.Parse("#FF0078D4"), color);
    }

    [AvaloniaFact]
    public void DesignTokens_DarkTheme_BorderAccent_IsCorrectColour()
    {
        var color = LookupColor("Border.Accent", ThemeVariant.Dark);
        Assert.Equal(Color.Parse("#FF3399FF"), color);
    }

    [AvaloniaFact]
    public void DesignTokens_AllLightColourTokens_ArePresentAndBrushes()
    {
        var keys = new[]
        {
            "Surface.Background", "Surface.Panel", "Surface.Titlebar", "Surface.TitlebarActive",
            "Border.Subtle", "Border.Accent",
            "Foreground.Primary", "Foreground.Secondary",
            "Accent.Receiving", "Accent.Paused", "Accent.Error", "Accent.Sparkline",
        };
        var app = Application.Current!;
        foreach (var key in keys)
        {
            app.TryGetResource(key, ThemeVariant.Light, out var res);
            Assert.True(res is ISolidColorBrush, $"Token '{key}' missing or wrong type in Light theme.");
        }
    }

    [AvaloniaFact]
    public void DesignTokens_AllDarkColourTokens_ArePresentAndBrushes()
    {
        var keys = new[]
        {
            "Surface.Background", "Surface.Panel", "Surface.Titlebar", "Surface.TitlebarActive",
            "Border.Subtle", "Border.Accent",
            "Foreground.Primary", "Foreground.Secondary",
            "Accent.Receiving", "Accent.Paused", "Accent.Error", "Accent.Sparkline",
        };
        var app = Application.Current!;
        foreach (var key in keys)
        {
            app.TryGetResource(key, ThemeVariant.Dark, out var res);
            Assert.True(res is ISolidColorBrush, $"Token '{key}' missing or wrong type in Dark theme.");
        }
    }

    [AvaloniaFact]
    public void DesignTokens_SpacingTokens_ArePresent()
    {
        var keys = new[] { "Spacing.Sm", "Spacing.Md", "Spacing.Lg", "Spacing.Xl", "Spacing.Xxl" };
        var expected = new[] { 4.0, 8.0, 12.0, 16.0, 24.0 };
        var app = Application.Current!;
        for (var i = 0; i < keys.Length; i++)
        {
            app.TryGetResource(keys[i], null, out var res);
            Assert.True(res is double, $"Spacing token '{keys[i]}' missing or wrong type.");
            Assert.Equal(expected[i], (double)res!);
        }
    }

    [AvaloniaFact]
    public void DesignTokens_TypographyTokens_ArePresent()
    {
        var app = Application.Current!;
        app.TryGetResource("FontSize.Body", null, out var body);
        app.TryGetResource("FontSize.Caption", null, out var caption);
        app.TryGetResource("FontSize.Mono", null, out var mono);
        Assert.Equal(13.0, body);
        Assert.Equal(12.0, caption);
        Assert.Equal(11.0, mono);
    }
}
