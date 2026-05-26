using System.Collections.Generic;
using System.Threading.Tasks;

namespace DdsMonitor.Avalonia.Core;

/// <summary>
/// Filter entry for file picker dialogs.
/// </summary>
/// <param name="Name">Human-readable filter name (e.g. "JSON files").</param>
/// <param name="Extensions">File extensions without the leading dot (e.g. "json", "txt").</param>
public record FilePickerFilter(string Name, IReadOnlyList<string> Extensions);

/// <summary>
/// Abstraction over platform file-open and file-save dialogs.
/// </summary>
public interface IFileDialogService
{
    /// <summary>Opens a file-picker dialog and returns the selected path, or <c>null</c> if cancelled.</summary>
    Task<string?> OpenFileAsync(string title, IReadOnlyList<FilePickerFilter> filters,
                                string? initialDirectory = null);

    /// <summary>Opens a save-file dialog and returns the chosen path, or <c>null</c> if cancelled.</summary>
    Task<string?> SaveFileAsync(string title, string suggestedName,
                                IReadOnlyList<FilePickerFilter> filters);
}
