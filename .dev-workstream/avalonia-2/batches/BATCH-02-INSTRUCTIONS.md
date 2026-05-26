# BATCH-02: Design Tokens, MdiChild, MdiHost

**Batch Number:** BATCH-02  
**Tasks:** DEBT-001 fix, M1-T2, M1-T3, M1-T4  
**Milestone:** M1 — Hybrid Shell, MDI Host, FeatureDemo  
**Estimated Effort:** 18–22 hours  
**Priority:** HIGH  
**Dependencies:** BATCH-01 (M1-T0, M1-T1 must be complete)

---

## 📋 Onboarding & Workflow

### Developer Instructions

BATCH-01 is complete and committed. This batch builds the visual layer (design
tokens + base styles) and the two custom controls at the heart of the MDI layout:
`MdiChild` and `MdiHost`. It also fixes a pre-existing test failure revealed
by BATCH-01.

### Required Reading (IN ORDER)

1. **Workflow Guide:** `.github/skills/developer/SKILL.md`.
2. **Design Document:** `.dev-workstream/avalonia-2/DESIGN.md` — focus on:
   - §4 (Custom MdiHost) — **authoritative spec** for both MdiChild and MdiHost.
   - §7 (Visual Design Tokens) — every token in the table is required.
   - §8 (Keyboard Navigation) — tab order and shortcut rules for MdiChild.
   - §11 (Testing Strategy).
   - §12 (Coding Conventions).
3. **Task Details:** `.dev-workstream/avalonia-2/TASK-DETAILS.md`:
   - Full spec for **M1-T2**, **M1-T3**, **M1-T4**.
4. **Previous Review:** `.dev-workstream/avalonia-2/reviews/BATCH-01-REVIEW.md`.
5. **DEBT-TRACKER:** `.dev-workstream/avalonia-2/DEBT-TRACKER.md` — DEBT-001 must
   be fixed in this batch.
6. **Blazor reference** for drag/resize math:
   `tools/DdsMonitor/DdsMonitor.Blazor/Components/Desktop.razor` (look for
   `BeginDrag`, `BeginResize`, `ApplyResize`, `EnsurePanelsVisibleAsync`).
7. **Existing shell project:** `tools/DdsMonitor/DdsMonitor.Avalonia/` — look
   at `App.axaml`, `App.axaml.cs` to understand the starting point for M1-T2.

### Source Code Locations

- **Design tokens + styles:** `tools/DdsMonitor/DdsMonitor.Avalonia/Themes/` *(create)*
- **MdiChild + MdiHost:** `tools/DdsMonitor/DdsMonitor.Avalonia/Controls/` *(create)*
- **Shell tests:** `tests/DdsMonitor.Avalonia.Tests/Controls/` *(create)*
- **PluginLoader (debt fix):** `tools/DdsMonitor/DdsMonitor.Engine/Plugins/PluginLoader.cs`

### Build & Test Commands

```powershell
# Build whole solution
dotnet build CycloneDDS.NET.sln -c Debug

# Run affected test projects
dotnet test tests/DdsMonitor.Avalonia.Tests/ -c Debug
dotnet test tests/DdsMonitor.Avalonia.Core.Tests/ -c Debug

# Quick sanity: only the new control tests
dotnet test tests/DdsMonitor.Avalonia.Tests/ -c Debug --filter "FullyQualifiedName~Controls"
```

### Report Submission

Submit your report to:
`.dev-workstream/avalonia-2/reports/BATCH-02-REPORT.md`

---

## Context

This batch produces:
1. **DEBT-001 fix**: Make `PluginLoader.TryLoadPluginFromFile` handle structural
   DLL errors (`BadImageFormatException`, `FileLoadException`) in all build configs.
2. **M1-T2**: The design-token ResourceDictionary and base control style overrides
   that every panel will reference.
3. **M1-T3**: `MdiChild` — the floating child window control (titlebar, resize handles,
   drag, z-order contribution, layout context menu).
4. **M1-T4**: `MdiHost` — the canvas that hosts multiple `MdiChild` instances, manages
   z-order, minimised strip, and boundary clamping.

M1-T5 (WindowManager) and M1-T6 (Dock.NET) depend directly on M1-T4 being available.

---

## 🎯 Batch Objectives

- `PluginLoader_CorruptDll_DoesNotCrash` test passes in Debug builds.
- All design tokens from DESIGN.md §7 colour table are present in `DesignTokens.axaml`
  (both Light and Dark variants).
- `BaseStyles.axaml` overrides Button, TextBox, Menu/MenuItem, Window per spec.
- `App.axaml` includes both theme files.
- `DesignTokensTests.cs` headless tests verify `Surface.Background` lookup for
  Light (`#FFFAFAFA`) and Dark (`#FF1A1A1A`).
- `MdiChild` control builds with all styled properties, events, drag/resize
  event delegation, and context menu.
- All `MdiChildTests` pass (7 tests per TASK-DETAILS.md §M1-T3).
- `MdiHost` control builds with canvas, z-order, minimised strip, and boundary
  clamping.
- All `MdiHostTests` pass (8 tests per TASK-DETAILS.md §M1-T4).
- Zero Dock.NET references in `MdiChild.cs`, `MdiChild.axaml`, `MdiHost.cs`,
  `MdiHost.axaml`.

---

## ✅ Tasks

### Task 0: DEBT-001 Fix — PluginLoader structural DLL exception handling

**Priority:** Fix before M1-T2 (low effort, unblocks CI green)

**File:** `tools/DdsMonitor/DdsMonitor.Engine/Plugins/PluginLoader.cs`

**Problem:** `TryLoadPluginFromFile` only catches exceptions in `#if !DEBUG`.
The test `PluginLoader_CorruptDll_DoesNotCrash` fails in Debug because
`BadImageFormatException` propagates uncaught.

**Fix:** Restructure the exception handling so structural DLL-load failures
(`BadImageFormatException`, `FileLoadException`) are ALWAYS caught and logged
(regardless of build config), while the broader `Exception` catch remains
`#if !DEBUG` only (to preserve fast-fail during development of logic errors).

**Target approach:**
```csharp
public int TryLoadPluginFromFile(string dllPath, IServiceCollection services)
{
    if (string.IsNullOrWhiteSpace(dllPath)) return 0;

    try
    {
        return LoadPluginFromFileCore(dllPath, services);
    }
    catch (BadImageFormatException ex)
    {
        _logger?.LogError(ex, "PluginLoader: '{Path}' is not a valid .NET assembly. Skipping.", dllPath);
        return 0;
    }
    catch (FileLoadException ex)
    {
        _logger?.LogError(ex, "PluginLoader: '{Path}' could not be loaded. Skipping.", dllPath);
        return 0;
    }
#if !DEBUG
    catch (Exception ex)
    {
        _logger?.LogError(ex, "PluginLoader: failed to load '{Path}'. Skipping.", dllPath);
        return 0;
    }
#endif
}
```

Apply the same pattern to `InitializePlugins` if it has the same `#if !DEBUG`
guard structure.

**Verify:** `dotnet test tests/DdsMonitor.Avalonia.Tests/ --filter PluginLoader`
should show green.

---

### Task 1: M1-T2 — Design Tokens and Base Styles

**Full spec:** See [TASK-DETAILS.md §M1-T2](../../TASK-DETAILS.md#m1-t2--design-tokens-and-base-styles)

**Summary:**

1. **Create** `tools/DdsMonitor/DdsMonitor.Avalonia/Themes/DesignTokens.axaml`:
   - A `ResourceDictionary` with `ThemeDictionaries` for `Light` and `Dark`.
   - Every row from DESIGN.md §7 colour table: `Surface.Background`,
     `Surface.Panel`, `Surface.Titlebar`, `Surface.TitlebarActive`,
     `Border.Subtle`, `Border.Accent`, `Foreground.Primary`,
     `Foreground.Secondary`, `Accent.Receiving`, `Accent.Paused`,
     `Accent.Error`, `Accent.Sparkline`.
   - Spacing tokens as `<x:Double>`: `Spacing.Sm=4`, `Spacing.Md=8`,
     `Spacing.Lg=12`, `Spacing.Xl=16`, `Spacing.Xxl=24`.
   - Typography tokens: `FontFamily.Body`, `FontFamily.Mono`,
     `FontSize.Body=13`, `FontSize.Caption=12`, `FontSize.Mono=11`.

2. **Create** `tools/DdsMonitor/DdsMonitor.Avalonia/Themes/BaseStyles.axaml`:
   - Override `Button`: flat 1 px `Border.Subtle` border, 2 px corner radius.
   - Override `TextBox`: flat 1 px border, focus uses `Border.Accent`.
   - Override `Menu`/`MenuItem`: no icon column, smaller padding.
   - Override `Window`: uses `Surface.Background`.
   - Keep overrides conservative — only properties differing from FluentTheme defaults.

3. **Modify** `App.axaml`:
   - First style: `<FluentTheme />`.
   - Second style: `<StyleInclude Source="avares://DdsMonitor.Avalonia/Themes/BaseStyles.axaml" />`.
   - In `Application.Resources.MergedDictionaries`: `<ResourceInclude Source="avares://DdsMonitor.Avalonia/Themes/DesignTokens.axaml" />`.

4. **Create** `tests/DdsMonitor.Avalonia.Tests/DesignTokensTests.cs`:
   - Use `Avalonia.Headless.XUnit` (`[AvaloniaFact]`).
   - `DesignTokens_LightTheme_SurfaceBackground_IsCorrectColour`: look up
     `Surface.Background` and assert it equals `Color.Parse("#FAFAFA")`.
   - `DesignTokens_DarkTheme_SurfaceBackground_IsCorrectColour`: switch to
     dark theme, assert `Surface.Background` equals `Color.Parse("#1A1A1A")`.
   - Note: The test setup needs to use Avalonia's theme variant API.

**⚠️ IMPORTANT:** The token `Surface.Background` Light value is `#FAFAFA`
(no alpha) — when stored as `SolidColorBrush`, the `Color` will be
`#FFFAFAFA` (alpha=FF added). Your tests must account for this.

**Acceptance criteria:**
- `dotnet build` zero errors.
- All tokens from DESIGN.md §7 present in `DesignTokens.axaml`.
- `DesignTokensTests` passes both light and dark assertions.
- `App.axaml` includes both files.

---

### Task 2: M1-T3 — MdiChild Custom Control

**Full spec:** See [TASK-DETAILS.md §M1-T3](../../TASK-DETAILS.md#m1-t3--mdichild-custom-control)

**Summary — what to build:**

All under `tools/DdsMonitor/DdsMonitor.Avalonia/Controls/`.

**`MdiChild.cs`** — `ContentControl` subclass:
- Styled properties: `Title` (string), `IsActive` (bool), `IsMinimised` (bool, two-way),
  `TitlebarExtras` (object), `ChildId` (string).
- Routed events: `CloseRequested`, `MinimiseRequested`, `LayoutKindRequested`,
  `BringToFrontRequested`.
- Internal delegation events: `DragRequested(double dx, double dy)`,
  `ResizeRequested(ResizeEdge edge, double dx, double dy)`, `DragCancelled`.
- Co-located types:
  - `MdiChildLayoutKindEventArgs : RoutedEventArgs { LayoutKind TargetKind; }`
  - `[Flags] enum ResizeEdge { None=0, Top=1, Right=2, Bottom=4, Left=8 }`

**Interaction logic in `MdiChild.cs`:**
- Titlebar left-press → capture pointer → on PointerMoved fire `DragRequested(dx, dy)`.
  `Shift` held → snap to 8 px grid. On `PointerReleased` end drag.
- `Escape` during drag → fire `DragCancelled`, revert to saved start position.
- Resize handle press → capture → on PointerMoved fire `ResizeRequested(edge, dx, dy)`.
- `Escape` during resize → fire `DragCancelled` (same event reused), revert size.
- Titlebar right-click → open `ContextMenu` with items per TASK-DETAILS.md §M1-T3.

**`MdiChild.axaml`** — default `ControlTemplate`:
- Outer `Border` with `BorderBrush={DynamicResource Border.Subtle}` (or `Border.Accent` when active), `BorderThickness="1"`, `Background={DynamicResource Surface.Panel}`.
- `Grid RowDefinitions="28,*"`: Row 0 titlebar, Row 1 content.
- Titlebar `Border` background `{DynamicResource Surface.Titlebar}` (active: `TitlebarActive`).
- DockPanel in titlebar: `TextBlock` (title, 8px padding), right-docked
  `StackPanel` (TitlebarExtras ContentPresenter, `_` button, `✕` button).
- Content row: `ContentPresenter`.
- Eight resize handle `Border`s with transparent background, correct cursor hints,
  `Tag` set to `"TopLeft"`, `"Top"`, `"TopRight"`, `"Right"`, `"BottomRight"`, etc.

**Tests `tests/DdsMonitor.Avalonia.Tests/Controls/MdiChildTests.cs`:**
All `[AvaloniaFact]` (need headless window):
1. `IsActive_True_SetsActiveClass` — set `IsActive=true`; assert `child.Classes.Contains("active")`.
2. `CloseButton_Click_RaisesCloseRequested` — find `✕` button, click it, assert `CloseRequested` fired.
3. `MinimiseButton_Click_RaisesMinimiseRequested` — find `_` button, click it, assert `MinimiseRequested` fired.
4. `TitlebarRightClick_OpensContextMenuWithSixItems` — right-click titlebar, assert `ContextMenu` opens with ≥ 6 items.
5. `Drag_FiftyPxRight_FiresDragRequestedWithDelta` — simulate pointer press + move 50 px right + release; assert `DragRequested` was raised with `dx ≈ 50, dy ≈ 0`.
6. `Resize_BottomRight_FiresResizeRequestedWithCorrectEdge` — press on BottomRight handle, move (100,50), release; assert `ResizeRequested(BottomRight, 100, 50)` raised.
7. `Escape_DuringDrag_FiresDragCancelled` — start drag, press Escape; assert `DragCancelled` fired.

**⚠️ Key design points from TASK-DETAILS.md:**
- The host (not MdiChild) applies drag/resize to `Canvas.Left/Top`. MdiChild only raises events.
- `MdiChild` does NOT dispose the DataContext when closed — the host does.
- No Dock.NET references allowed.

---

### Task 3: M1-T4 — MdiHost Custom Control

**Full spec:** See [TASK-DETAILS.md §M1-T4](../../TASK-DETAILS.md#m1-t4--mdihost-custom-control)

**Summary — what to build:**

All under `tools/DdsMonitor/DdsMonitor.Avalonia/Controls/`.

**`MdiHost.cs`** — `TemplatedControl` subclass (or `Control`):

Public API (per spec):
```csharp
void Add(MdiChild child, double x, double y, double width, double height);
bool Remove(string childId);
bool TryGet(string childId, out MdiChild child);
IReadOnlyList<MdiChild> Children { get; }
void BringToFront(string childId);
void Minimise(string childId);
void Restore(string childId);
void FocusNext(bool reverse);
event EventHandler<MdiChildEventArgs>? ChildAdded;
event EventHandler<MdiChildEventArgs>? ChildRemoved;
event EventHandler<MdiChildGeometryEventArgs>? ChildGeometryChanged;
```

Implementation requirements:
- `Add`: set `Canvas.Left/Top/Width/Height`, add to `PART_ChildCanvas.Children`,
  subscribe to child events (`DragRequested`, `ResizeRequested`, `CloseRequested`,
  `MinimiseRequested`, `BringToFrontRequested`), call `BringToFront`.
- **Drag handler**: apply `Canvas.Left += dx; Canvas.Top += dy`, then clamp so
  titlebar (top 28 px) stays ≥ 40 px inside host bounds. Fire `ChildGeometryChanged`.
- **Resize handler**: apply delta to child dimensions, enforce min 220×140.
  Fire `ChildGeometryChanged`.
- **BringToFront**: `Interlocked.Increment(ref _zCounter)`, set `Canvas.SetZIndex`.
- **Minimise**: set `child.IsVisible = false`, add a restore `Button` to
  `PART_MinimisedStrip.Items` (or add to its ItemsSource).
- **Restore**: set `child.IsVisible = true`, remove the button from the strip.
- **FocusNext**: cycle through `Children` by `Canvas.ZIndex` order.
- **Boundary clamp on host resize**: subscribe `LayoutUpdated`; for each visible
  child, if `Canvas.GetLeft(child) + child.MinWidth > Bounds.Width - 40` or similar,
  move child inward.

**`MdiHost.axaml`** — `ControlTemplate`:
```xml
<DockPanel>
  <ItemsControl x:Name="PART_MinimisedStrip" DockPanel.Dock="Bottom"
                Background="{DynamicResource Surface.Titlebar}"
                IsVisible="{Binding ???}">
    <!-- ItemTemplate: Button per minimised child -->
  </ItemsControl>
  <Canvas x:Name="PART_ChildCanvas" Background="{DynamicResource Surface.Background}" />
</DockPanel>
```
`PART_MinimisedStrip` is only visible when items count > 0. The strip background
is `Surface.Titlebar` and height is 32 px.

**Tests `tests/DdsMonitor.Avalonia.Tests/Controls/MdiHostTests.cs`:**
All `[AvaloniaFact]`:
1. `Add_ThreeChildren_CountIsThree` — add 3 children; assert `Children.Count == 3`.
2. `BringToFront_RaisesZIndexAboveSiblings` — add 2; bring child1 to front; assert
   `Canvas.GetZIndex(child1) > Canvas.GetZIndex(child2)`.
3. `Drag_PastRightEdge_Clamps` — host width=500; add child at x=450, width=200;
   drag 100 px right; assert `Canvas.GetLeft(child) ≤ 460` (keeps ≥ 40 px inside).
4. `Resize_BelowMinWidth_ClampsTo220` — resize child with `dx=-1000`; assert
   child `Width >= 220`.
5. `Minimise_ChildIsNotVisible_StripHasOneItem` — minimise child; assert
   `child.IsVisible == false` AND strip has 1 item.
6. `Restore_ChildBecomesVisible_StripIsEmpty` — minimise then restore; assert
   `child.IsVisible == true` AND strip has 0 items.
7. `FocusNext_WithThreeChildren_CyclesCorrectly` — add 3 children; call
   `FocusNext(false)` twice; verify focus advances without throwing.
8. `Remove_MissingId_ReturnsFalse` — call `Remove("nonexistent")`;
   assert returns `false`; `Children.Count` unchanged.

---

## 🧪 Testing Requirements

**Minimum tests (non-negotiable):**
- 1 `PluginLoader_CorruptDll_DoesNotCrash` fix verified
- 2 `DesignTokensTests` (light + dark `Surface.Background`)
- 7 `MdiChildTests` (all from spec above)
- 8 `MdiHostTests` (all from spec above)

**Quality standards:**
- **MdiChild drag tests**: must simulate actual pointer events (use
  `RaiseEvent` with `PointerPressedEventArgs`, `PointerMovedEventArgs`,
  `PointerReleasedEventArgs`). Do NOT just call internal methods directly.
- **MdiHost geometry tests**: verify the actual `Canvas.GetLeft`/`Canvas.GetTop`
  values, not just that no exception was thrown.
- **Event-raised tests**: use a local flag + event subscription — do NOT check
  state after the fact without verifying the event fired.
- Tests must be deterministic (no randomness, no `Task.Delay`).

---

## 📊 Developer Insights Required in Report

1. **Drag/resize pointer simulation**: What Avalonia API did you use to simulate
   pointer events in headless tests? Did `KeyboardDevice.Instance` work or did
   you need a different approach?

2. **ContextMenu right-click test**: Describe exactly how you verified the context
   menu appeared with the correct items in headless mode.

3. **`PART_MinimisedStrip` visibility binding**: How did you bind strip visibility
   to "has at least one minimised child"? (Direct property? Collection count?)

4. **Design token test approach**: How did you switch the theme to Dark in headless
   mode to test the dark `Surface.Background` value?

5. **Weak points spotted**: Any concerns about the architecture or edge cases in
   the codebase that aren't covered in this batch.

6. **Design decisions beyond spec**: Any implementation choices not in TASK-DETAILS.md.

---

## 📋 Report Format

```markdown
# BATCH-02 Report

## Tasks Completed
- DEBT-001: [status]
- M1-T2: [status]
- M1-T3: [status]
- M1-T4: [status]

## Build & Test Results
[Full output: dotnet build CycloneDDS.NET.sln -c Debug]
[Full output: dotnet test tests/DdsMonitor.Avalonia.Tests/ -c Debug]
[Full output: dotnet test tests/DdsMonitor.Avalonia.Core.Tests/ -c Debug]

## Developer Insights
[Answer all 6 questions above]

## Files Changed
[Complete list]

## Known Issues
[Any deferred items]
```

---

## 🔄 Mandatory Workflow: Test-Driven Task Progression

Follow this for each task:

1. Read the full task spec from TASK-DETAILS.md before writing any code.
2. Write the test(s) first; confirm they fail.
3. Implement production code to make tests pass.
4. Run `dotnet build` — zero errors required.
5. Run `dotnet test` for affected projects — all green.
6. Move to next task.

**Critical:** Tests for MdiChild and MdiHost are non-trivial headless Avalonia tests.
If you hit difficulty with the pointer event simulation, do not fake the tests —
use Avalonia's documented headless testing APIs. The `Avalonia.Headless.XUnit`
package includes helpers for simulating input.
