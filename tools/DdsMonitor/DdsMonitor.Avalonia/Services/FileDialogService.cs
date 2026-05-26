using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using DdsMonitor.Avalonia.Core;

namespace DdsMonitor.Avalonia.Services;

public sealed class FileDialogService : IFileDialogService
{
    private readonly Func<Visual?> _rootProvider;

    public FileDialogService(Func<Visual?> rootProvider)
    {
        _rootProvider = rootProvider;
    }

    public async Task<string?> OpenFileAsync(string title,
                                              IReadOnlyList<FilePickerFilter> filters,
                                              string? initialDirectory = null)
    {
        var topLevel = GetTopLevel();
        if (topLevel is null) return null;

        var avFilters = BuildFileTypes(filters);

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title          = title,
                FileTypeFilter = avFilters,
                AllowMultiple  = false,
            });

        return files.Count > 0 ? files[0].Path.LocalPath : null;
    }

    public async Task<string?> SaveFileAsync(string title,
                                              string suggestedName,
                                              IReadOnlyList<FilePickerFilter> filters)
    {
        var topLevel = GetTopLevel();
        if (topLevel is null) return null;

        var avFilters = BuildFileTypes(filters);

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(
            new FilePickerSaveOptions
            {
                Title             = title,
                SuggestedFileName = suggestedName,
                FileTypeChoices   = avFilters,
            });

        return file?.Path.LocalPath;
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private TopLevel? GetTopLevel()
    {
        var root = _rootProvider();
        return root is null ? null : TopLevel.GetTopLevel(root);
    }

    private static List<FilePickerFileType> BuildFileTypes(IReadOnlyList<FilePickerFilter> filters) =>
        filters
            .Select(f => new FilePickerFileType(f.Name)
            {
                Patterns = f.Extensions.Select(e => $"*.{e}").ToList(),
            })
            .ToList();
}
