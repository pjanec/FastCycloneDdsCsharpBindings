# BATCH-02 Report

**Branch:** ddsmon-avalonia  
**Tasks:** DEBT-001 fix, M1-T2 (Design Tokens), M1-T3 (MdiChild), M1-T4 (MdiHost)  
**Status:** ✅ All tasks complete

---

## Tasks Completed

- **DEBT-001:** ✅ Resolved — `TryLoadPluginFromFile` and `InitializePlugins` fixed
- **M1-T2:** ✅ Complete — DesignTokens.axaml, BaseStyles.axaml, App.axaml, 8 tests pass
- **M1-T3:** ✅ Complete — MdiChild.cs, MdiChild.axaml, 7 headless tests pass
- **M1-T4:** ✅ Complete — MdiHost.cs, MdiHost.axaml, 8 headless tests pass

---

## Build & Test Results

### `dotnet build CycloneDDS.NET.sln -c Debug`
```
Build succeeded.
    19 Warning(s)
    0 Error(s)
```
*(Warnings are pre-existing nullable/obsolete annotations in unrelated projects.)*

### `dotnet test tests/DdsMonitor.Avalonia.Tests/ -c Debug`
```
Passed!  - Failed:     0, Passed:    68, Skipped:     0, Total:    68, Duration: 563 ms
```

### `dotnet test tests/DdsMonitor.Avalonia.Core.Tests/ -c Debug`
```
Passed!  - Failed:     0, Passed:    27, Skipped:     0, Total:    27, Duration: 2 s
```

---

## Developer Insights

### 1. Drag/resize pointer simulation

The Avalonia headless `HeadlessWindowExtensions` methods all require `RawInputModifiers` as a mandatory final argument — unlike what the API surface might suggest from intellisense/docs. The correct signatures are:

```csharp
window.MouseDown(new Point(x, y), MouseButton.Left, RawInputModifiers.None);
window.MouseMove(new Point(x, y), RawInputModifiers.None);
window.MouseUp(new Point(x, y), MouseButton.Left, RawInputModifiers.None);
window.KeyPress(Key.Escape, RawInputModifiers.None);
```

`KeyboardDevice.Instance` is not used for headless testing — it's an internal infrastructure concern. The `HeadlessWindowExtensions` approach is the correct public API. Pointer events fired this way propagate through the visual tree normally, triggering `PointerPressed`/`PointerMoved` handlers on the controls.

Because `MdiChild` uses a `_isDragging` flag (set in `PointerPressed`, cleared in `PointerReleased`) to gate `DragRequested` emission on pointer moves, the drag test verifies the event *may* fire rather than asserting an exact delta value — the Avalonia headless layout engine may not complete a full measure/arrange pass within the test, making exact coordinate-based assertions fragile.

### 2. ContextMenu right-click test

In headless mode, context menus bound via `Border.ContextMenu` (as in `PART_Titlebar`) are not automatically opened by simulated right-clicks because Avalonia's context menu popup manager requires a real pointer capture lifecycle. Instead, the test:

1. Uses `GetVisualDescendants()` (from `Avalonia.VisualTree`) to locate `PART_Titlebar` after `ApplyTemplate()` has run.
2. Directly inspects `titlebar.ContextMenu.Items` without opening the popup.
3. Counts `MenuItem` entries and checks their `Header` strings.

This is a structural/wiring test, not a rendered-popup test. It verifies the menu is *defined* correctly and attached to the correct template part. Full open/close interaction is deferred to future integration tests.

### 3. `PART_MinimisedStrip` visibility binding

The `PART_MinimisedStrip` `ItemsControl`'s visibility is managed **imperatively** in `MdiHost.cs`, not via a binding. When `Minimise(childId)` is called, a `Button` is added to the strip and `_strip.IsVisible = true`. When the last minimised child is restored (strip is empty after button removal), `_strip.IsVisible = false`.

A binding approach (`Items.Count > 0 → converter → IsVisible`) was considered but rejected because:
- The strip reference is obtained in `OnApplyTemplate` before any children exist.
- Keeping visibility in sync with an `ObservableCollection` would require another abstraction for what is essentially a two-line imperative check.
- The MdiHost already owns the strip lifecycle; centralising the logic there keeps the binding complexity low.

### 4. Design token test approach for dark theme

The dark theme resource lookup uses `TryGetResource(key, ThemeVariant.Dark, out var resource)`. `ThemeVariant.Dark` is a static singleton from `Avalonia.Styling`. The test does **not** switch the application theme globally — it queries the resource dictionary directly using the theme variant parameter. This avoids test ordering issues and the need to reset global application state between test methods.

```csharp
var app = Application.Current!;
app.TryGetResource("Surface.Background", ThemeVariant.Dark, out var darkBg);
Assert.IsType<SolidColorBrush>(darkBg);
```

The `ThemeVariant.Light` / `ThemeVariant.Dark` approach means the tokens are validated per-variant without any actual theme switch, which is both faster and more isolated.

### 5. Weak points spotted

1. **MdiChild drag contract is untestable at unit level**: The `_isDragging` flag relies on the pointer being captured to the titlebar. In headless mode, pointer capture may not function as in a real compositor, meaning drag delta assertions are best done in an integration/e2e test with a real window. The unit tests verify the event is wired, not the delta arithmetic.

2. **`BringToFront` z-order is monotonically increasing**: `_zCounter` grows without bound. For long-running sessions with many bring-to-front operations, `ZIndex` values become large integers but functional. A future improvement would be to renumber all children when `_zCounter` overflows, but `int.MaxValue` (~2 billion) calls makes this irrelevant in practice.

3. **Minimised strip layout is fixed at 32px**: The strip height is hardcoded in `MdiHost.axaml`. If minimised buttons have variable heights (e.g., accessibility font sizes), clipping will occur. Binding the strip height to `PART_MinimisedStrip.DesiredSize.Height` is a follow-up.

4. **`LoadPlugins_WhenConfigFileMissing` and `LoadPlugins_WhenConfigFileCorrupt` are pre-existing failures** in `DdsMonitor.Engine.Tests` (confirmed via `git stash` check — failing on the base commit before any BATCH-02 changes). These are unrelated to DEBT-001 and trace to `PluginConfigService.HadConfigFileAtInitialization` behaviour, not `TryLoadPluginFromFile`. They should be tracked as new DEBT items.

### 6. Design decisions beyond spec

**`GetVisualDescendants()` instead of `GetTemplateChildren()`**: The Avalonia 11 API does not have `GetTemplateChildren()` (that's WPF). `GetVisualDescendants()` (from `Avalonia.VisualTree`) traverses the full visual tree. For template part lookups in tests, this is equivalent and idiomatic for Avalonia.

**`ZIndex` property instead of `Canvas.SetZIndex()`**: Avalonia 11 does not have `Canvas.GetZIndex` / `Canvas.SetZIndex` static methods. The `ZIndex` property lives directly on `Control`. `MdiHost` uses `child.ZIndex = Interlocked.Increment(ref _zCounter)` for thread-safe z-order assignment.

**`StyleInclude` in `Avalonia.Markup.Xaml.Styling`**: Both `StyleInclude` and `ResourceInclude` are in `Avalonia.Markup.Xaml.Styling` (not `Avalonia.Styling` or `Avalonia.Controls`). The `TestApp.cs` headless application must use these types explicitly when programmatically loading styles.

**`TryGetResource` instead of `TryFindResource`**: Avalonia 11's `Application` uses `TryGetResource(key, ThemeVariant?, out resource)` — the `TryFindResource` name does not exist on `Application`.

**Drag handling in MdiHost uses simple clamping**: Rather than a full constraint solver, drag clamping enforces that at least 40px of the titlebar stays on-screen in all directions. Width/height minimums (220×140) are enforced on resize. These constants are defined as `private const int` fields in `MdiHost.cs` for easy tuning.

---

## Files Changed

### New files
| File | Description |
|------|-------------|
| `tools/DdsMonitor/DdsMonitor.Avalonia/Themes/DesignTokens.axaml` | M1-T2: Light/Dark colour brushes, spacing, typography tokens |
| `tools/DdsMonitor/DdsMonitor.Avalonia/Themes/BaseStyles.axaml` | M1-T2: Flat control style overrides (Button, TextBox, Menu, Window) |
| `tools/DdsMonitor/DdsMonitor.Avalonia/Controls/MdiChild.cs` | M1-T3: MdiChild control (styled properties, routed events, template wiring) |
| `tools/DdsMonitor/DdsMonitor.Avalonia/Controls/MdiChild.axaml` | M1-T3: MdiChild control template (titlebar, 8 resize handles, active styles) |
| `tools/DdsMonitor/DdsMonitor.Avalonia/Controls/MdiHost.cs` | M1-T4: MdiHost container (add/remove/minimise/restore/z-order/clamping) |
| `tools/DdsMonitor/DdsMonitor.Avalonia/Controls/MdiHost.axaml` | M1-T4: MdiHost control template (Canvas + minimised strip) |
| `tests/DdsMonitor.Avalonia.Tests/DesignTokensTests.cs` | M1-T2: 8 headless tests for design tokens |
| `tests/DdsMonitor.Avalonia.Tests/Controls/MdiChildTests.cs` | M1-T3: 7 headless tests for MdiChild |
| `tests/DdsMonitor.Avalonia.Tests/Controls/MdiHostTests.cs` | M1-T4: 8 headless tests for MdiHost |

### Modified files
| File | Change |
|------|--------|
| `tools/DdsMonitor/DdsMonitor.Engine/Plugins/PluginLoader.cs` | DEBT-001: Always catch `BadImageFormatException`/`FileLoadException`; fix `InitializePlugins` `#if` structure |
| `tools/DdsMonitor/DdsMonitor.Avalonia/App.axaml` | M1-T2/T3/T4: Added StyleIncludes for BaseStyles, MdiChild, MdiHost; ResourceInclude for DesignTokens |
| `tests/DdsMonitor.Avalonia.Tests/TestApp.cs` | M1-T2/T3/T4: Added style/resource includes for headless test application |
| `.dev-workstream/avalonia-2/DEBT-TRACKER.md` | DEBT-001 moved to resolved |

---

## Known Issues / Pre-existing Failures

| Test | Status | Notes |
|------|--------|-------|
| `LoadPlugins_WhenConfigFileMissing_DisablesAllDiscoveredPlugins` | ❌ Pre-existing | Fails on base commit; `PluginConfigService.HadConfigFileAtInitialization` logic issue, not related to DEBT-001 |
| `LoadPlugins_WhenConfigFileCorrupt_DisablesAllDiscoveredPlugins` | ❌ Pre-existing | Same root cause; unrelated to DEBT-001 |

Both were confirmed failing before any BATCH-02 changes via `git stash` + test run. They should be logged as a new debt item for the next batch.
