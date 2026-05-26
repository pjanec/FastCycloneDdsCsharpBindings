using System.Threading.Tasks;

namespace DdsMonitor.Avalonia.Core;

/// <summary>
/// Abstraction over the system clipboard for text operations.
/// </summary>
public interface IClipboardService
{
    /// <summary>Writes <paramref name="text"/> to the clipboard.</summary>
    Task SetTextAsync(string text);

    /// <summary>Returns the current clipboard text, or <c>null</c> if the clipboard is empty or unavailable.</summary>
    Task<string?> GetTextAsync();
}
