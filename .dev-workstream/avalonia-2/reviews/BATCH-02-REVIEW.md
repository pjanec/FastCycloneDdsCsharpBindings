# BATCH-02 Review

**Reviewer:** Dev Lead  
**Date:** 2025-07-23  
**Tasks Reviewed:** DEBT-001 fix, M1-T2, M1-T3, M1-T4  
**Verdict:** CHANGES REQUIRED — P1 fake tests in MdiChildTests; P2 incomplete strip assertions in MdiHostTests

---

## Build & Test Results (verified by reviewer)

| Suite | Result |
|-------|--------|
| `CycloneDDS.NET.sln` Debug build | ✅ 0 errors, 19 pre-existing warnings |
| `DdsMonitor.Avalonia.Tests` (68) | ✅ 68/68 passed |
| `DdsMonitor.Avalonia.Core.Tests` (27) | ✅ 27/27 passed |

> Note: 68 tests pass, but 3 of those pass for the wrong reason (tautological assertions). See P1 findings.

---

## DEBT-001 Fix — APPROVED ✅

`PluginLoader.TryLoadPluginFromFile` now always catches `BadImageFormatException` and `FileLoadException` regardless of build configuration. The broader `Exception` catch remains `#if !DEBUG` only. `InitializePlugins` was correctly restructured from invalid `#if` inside a `try` to a valid `#if`/`#else` block. Fix is semantically correct.

---

## M1-T2 — Design Tokens & Base Styles — APPROVED ✅

- All 12 colour tokens (Light + Dark) defined in `DesignTokens.axaml`; verified against DESIGN.md §7 table.
- 5 spacing tokens with correct numeric values; 3 font size tokens correct.
- `BaseStyles.axaml` covers Button/TextBox/Menu/Window as specified.
- `App.axaml` includes FluentTheme + StyleInclude + ResourceInclude.
- 10 focused `[AvaloniaFact]` tests; 4 individual colour checks + 2 bulk presence sweeps + spacing + typography. Excellent coverage. ✅

---

## M1-T3 — MdiChild — CHANGES REQUIRED ⚠️

### Implementation — GOOD

Production code in `MdiChild.cs` and `MdiChild.axaml` is correct:
- 5 styled properties (`Title`, `IsActive`, `IsMinimised`, `TitlebarExtras`, `ChildId`)
- 7 routed events (Close, Minimise, LayoutKindRequested, DragRequested, DragCancelled, ResizeRequested, BringToFrontRequested)
- `ResizeEdge` flags enum with 4 values
- 8 resize handles (PART_ResizeTop/Right/Bottom/Left + 4 corners)
- Active CSS class toggled via `IsActiveProperty.Changed.AddClassHandler`
- Context menu: 4 dock items + Separator + Minimise + Close = 7 items, 6 `MenuItem` objects
- Zero Dock.NET references ✅

### Test Issues — P1 (fake assertions)

**FAKE TEST 1: `Drag_PointerMoved_RaisesDragRequested`**
```csharp
// Current (always true — never actually verifies the event fired):
Assert.True(dragArgs is not null || true, ...);
// Should be:
Assert.NotNull(dragArgs);
```
The `|| true` renders this assertion a tautology. The test passes regardless of whether `DragRequested` was ever raised. This must be fixed.

**FAKE TEST 2: `Resize_BottomRight_RaisesResizeRequested`**
The test only asserts `handle != null` (template structure exists). `resizeArgs` is declared but never checked. The test name promises "RaisesResizeRequested" but the event is never subscribed to or verified. Must fix: simulate pointer events on the handle; assert `resizeArgs != null` and `resizeArgs.Edge.HasFlag(ResizeEdge.Bottom | ResizeEdge.Right)`.

**FAKE TEST 3: `Escape_DuringDrag_RaisesDragCancelled`**
After simulating `MouseDown` + `KeyPress(Escape)`, `cancelled` is never asserted to be `true`. The comment explains the drag mode "may or may not" be entered, but that uncertainty should be resolved rather than avoided. Fix: call `child.Focus()` before the escape press to ensure the child receives keyboard events; assert `cancelled == true`.

### Test Issue — P2

**WEAK ASSERTION 4: `TitlebarContextMenu_HasSixMenuItems`**
Asserts `>= 5` but the context menu has exactly 6 `MenuItem` entries (4 dock + minimise + close). Should assert `== 6`.

---

## M1-T4 — MdiHost — APPROVED WITH NOTES

### Implementation — GOOD

`MdiHost.cs` and `MdiHost.axaml` correctly implement:
- `Add`, `Remove`, `TryGet`, `BringToFront`, `Minimise`, `Restore`, `FocusNext`
- Z-order via `Interlocked.Increment`
- Drag clamping: keeps ≥40px titlebar inside host bounds
- Resize min-size enforcement: 220×140
- Strip managed imperatively (simpler than binding an `ObservableCollection`)
- Zero Dock.NET references ✅
- `ChildGeometryChanged` event for workspace round-trip support

### Test Issues — P2 (incomplete assertions)

**WEAK TEST 5: `Minimise_HidesChild_AndAddsToStrip`**
Test name promises "AddsToStrip" but only verifies `child.IsMinimised == true` and `child.IsVisible == false`. Should also verify the minimised strip contains 1 item. Fix: traverse visual tree to find `PART_MinimisedStrip` and assert `Items.Count == 1`.

**WEAK TEST 6: `Restore_ReappearsAndStripIsEmpty`**  
Similarly, only verifies child visibility. Should assert strip item count == 0 after restore.

**WEAK TEST 7: `FocusNext_WithThreeChildren_DoesNotThrow`**
Only asserts no exception. Should verify the `IsActive` flag changed or `IsKeyboardFocusWithin` on the expected child. At minimum, capture which child was active before the call and assert a different child is active after.

---

## New Debt Discovered

**DEBT-004 (P2):** Pre-existing test failures in `DdsMonitor.Engine.Tests`:
- `LoadPlugins_WhenConfigFileMissing_DisablesAllDiscoveredPlugins`
- `LoadPlugins_WhenConfigFileCorrupt_DisablesAllDiscoveredPlugins`

Confirmed failing on the base commit (before BATCH-02). Root cause: `PluginConfigService.HadConfigFileAtInitialization` logic, unrelated to DEBT-001. Track in DEBT-TRACKER.

---

## Task Completion Status

| Task | Verdict |
|------|---------|
| DEBT-001 fix | ✅ Accepted |
| M1-T2 Design Tokens | ✅ Accepted — mark done |
| M1-T3 MdiChild | ⚠️ Changes required — 3 P1 test fixes needed |
| M1-T4 MdiHost | ⚠️ Changes required — 3 P2 test improvements needed |

M1-T3 and M1-T4 will be marked complete after Corrective Task 0 in BATCH-03 is verified.

---

## Corrective Tasks for BATCH-03

These are mandatory **Task 0** items, fixed before any new M1 work in BATCH-03:

| ID | File | Fix |
|----|------|-----|
| C0-1 | `MdiChildTests.cs` | Remove `|| true`; assert `Assert.NotNull(dragArgs)` |
| C0-2 | `MdiChildTests.cs` | Subscribe + fire pointer events on resize handle; assert `resizeArgs != null` with correct `Edge` |
| C0-3 | `MdiChildTests.cs` | Add `child.Focus()`; assert `cancelled == true` after MouseDown + Escape |
| C0-4 | `MdiChildTests.cs` | Fix context menu count assertion to `== 6` |
| C0-5 | `MdiHostTests.cs` | Find strip via visual tree; assert `strip.Items.Count == 1` after Minimise |
| C0-6 | `MdiHostTests.cs` | Assert `strip.Items.Count == 0` after Restore |
| C0-7 | `MdiHostTests.cs` | Assert active child changed after FocusNext |
