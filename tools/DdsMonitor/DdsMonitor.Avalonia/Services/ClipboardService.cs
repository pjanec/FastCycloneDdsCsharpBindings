using Avalonia.Controls;
using DdsMonitor.Avalonia.Core;

namespace DdsMonitor.Avalonia.Services;

public sealed class ClipboardService : IClipboardService
{
    private readonly Func<TopLevel?> _topLevelProvider;

    public ClipboardService(Func<TopLevel?> topLevelProvider)
    {
        _topLevelProvider = topLevelProvider;
    }

    public async Task SetTextAsync(string text)
    {
        var clipboard = _topLevelProvider()?.Clipboard;
        if (clipboard is not null)
            await clipboard.SetTextAsync(text);
    }

    public async Task<string?> GetTextAsync()
    {
        var clipboard = _topLevelProvider()?.Clipboard;
        return clipboard is not null ? await clipboard.GetTextAsync() : null;
    }
}
