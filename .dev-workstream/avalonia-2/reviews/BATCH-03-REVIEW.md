# BATCH-03 Review

**Reviewer:** Dev Lead  
**Date:** 2025-07-23  
**Tasks Reviewed:** Corrective T0 (BATCH-02 P1/P2 test fixes), M1-T5 (AvaloniaWindowManager), M1-T6 (Dock.NET integration)  
**Verdict:** CHANGES REQUIRED — 1 P1 missing acceptance-criterion test; 2 P2 missing tests

---

## Build & Test Results (verified by reviewer)

| Suite | Result |
|-------|--------|
| `CycloneDDS.NET.sln` Debug build | ✅ 0 errors |
| `DdsMonitor.Avalonia.Tests` (78) | ✅ 78/78 passed |
| `DdsMonitor.Avalonia.Core.Tests` (27) | ✅ 27/27 passed |

---

## Corrective T0 — APPROVED ✅

All 7 corrective items from the BATCH-02 review are properly fixed:

| Item | Test | Fix Applied |
|------|------|-------------|
| C0-1 | `Drag_PointerMoved_RaisesDragRequested` | Real `Assert.NotNull(dragArgs)` via headless pointer simulation ✅ |
| C0-2 | `Resize_BottomRight_RaisesResizeRequested` | Subscribes, simulates, asserts `resizeArgs != null` + `Edge` flags ✅ |
| C0-3 | `Escape_DuringDrag_RaisesDragCancelled` | Reflection-forced drag mode + `Assert.True(cancelled)` ✅ |
| C0-4 | `TitlebarContextMenu_HasSixMenuItems` | `Assert.Equal(6, menuItems.Count)` ✅ |
| C0-5 | `Minimise_HidesChild_AndAddsToStrip` | Visual-tree strip lookup + `Assert.Equal(1, strip.Items.Count)` ✅ |
| C0-6 | `Restore_ReappearsAndStripIsEmpty` | Visual-tree strip lookup + `Assert.Equal(0, strip.Items.Count)` ✅ |
| C0-7 | `FocusNext_WithThreeChildren_DoesNotThrow` | Z-order change assertion (`frontBefore != frontAfter`) ✅ |

Bonus fix: `MdiChild` was not focusable by default; `FocusableProperty.OverrideDefaultValue<MdiChild>(true)` added so keyboard events route correctly. This was a latent bug that would have affected M1-T8 (key binding service). ✅

M1-T3 and M1-T4 are now fully accepted. Mark done.

---

## M1-T6 — Dock.NET Integration — APPROVED ✅

- `IDockManager.cs`: Clean interface with all required methods; `DockSide` enum (`Left, Right, Bottom`).
- `DdsDockFactory.cs`: Correct `Factory` subclass; builds layout tree (ProportionalDock → LeftToolDock + DocumentDock + RightToolDock, BottomToolDock). MDI Workspace document has `CanClose = false` and `CanFloat = false`. ✅
- `DockManager.cs`: Correct implementation; `internal` properties `DocumentDock/LeftToolDock/RightToolDock/BottomToolDock` exposed for testing.
- 4 `DockManagerTests` pass with real behavioral assertions (document count, tool dock item count, event fired on remove, round-trip JSON).
- Namespace issues (`IRootDock` in `Dock.Model.Controls`, `DockBase` in `Dock.Model.Mvvm.Core`) correctly resolved. ✅

**Note:** `Initialise` accepts `null` for `DockControl` gracefully (used in headless tests). This is intentional and documented. ✅

---

## M1-T5 — AvaloniaWindowManager — CHANGES REQUIRED ⚠️

### Implementation — GOOD

- `IAvaloniaWindowManager.cs`: Clean interface extending `IWindowManager`.
- `AvaloniaWindowManager.cs`: Rewrite is correct. MDI panels routed via `_mdiHost`, dock panels via `_dockManager`. `_layoutKinds` dictionary tracks placement without modifying engine types. `SetMdiHost` subscribes to `ChildRemoved` for correct close-chain handling.
- `LoadWorkspaceFromJson` correctly handles Blazor format: absent `LayoutKind` field defaults to `LayoutKind.Mdi`. ✅
- `WorkspaceSaveRequestedEvent` published after panel close. ✅
- `MoveToLayout` deferred (`NotSupportedException`) — acceptable for now; M1-T7 will wire the titlebar context menu event that triggers it.

### Missing Tests — P1 and P2

**MISSING P1: Blazor workspace compat test**

TASK-DETAILS.md acceptance criteria: *"The Blazor-workspace-compat test passes: a workspace JSON with no LayoutKind or DockLayout loads and produces only MDI children."*

This acceptance criterion is explicitly listed in the task spec and is missing. The implementation is correct (verified by code review) but the test is absent. A workspace migration test is critical for regression protection. Must be added.

**MISSING P2: `WorkspaceSaveRequestedEvent` published on close**

The implementation publishes the event (verified: line 442), but there is no test asserting this. Add a test subscribing to `eventBroker.Subscribe<WorkspaceSaveRequestedEvent>` and asserting it fires after `ClosePanel`.

**MISSING P2: SaveWorkspace/LoadWorkspace round-trip with LayoutKind**

The 6 tests in TASK-DETAILS.md included a round-trip test: `SaveWorkspaceToJson()` then `LoadWorkspaceFromJson(saved)` round-trips panel count, types, geometries, and `LayoutKind`. The test `SaveWorkspaceToJson_ContainsPanel` only partially covers this (contains panel id). A round-trip test is needed.

---

## Task Completion Status

| Task | Verdict |
|------|---------|
| Corrective T0 | ✅ Accepted — all 7 items fixed |
| M1-T3 MdiChild (pending from BATCH-02) | ✅ Accepted — mark done |
| M1-T4 MdiHost (pending from BATCH-02) | ✅ Accepted — mark done |
| M1-T6 Dock.NET Integration | ✅ Accepted — mark done |
| M1-T5 AvaloniaWindowManager | ⚠️ Changes required — 1 P1 + 2 P2 missing tests |

M1-T5 will be marked complete after Corrective Task 0 in BATCH-04 adds the missing tests.

---

## Corrective Tasks for BATCH-04

| ID | File | Fix |
|----|------|-----|
| C0-1 | `AvaloniaWindowManagerTests.cs` | Add `LoadWorkspaceFromJson_BlazorFormat_AllMdi` test: parse workspace JSON without `LayoutKind`/`DockLayout` fields; assert `mdiHost.Children.Count == 2` (2 panels spawned as MDI) |
| C0-2 | `AvaloniaWindowManagerTests.cs` | Add `ClosePanel_PublishesWorkspaceSaveRequestedEvent` test: subscribe via `StubEventBroker`; close panel; assert event was published |
| C0-3 | `AvaloniaWindowManagerTests.cs` | Add `SaveAndLoad_RoundTrips_LayoutKind` test: spawn 1 MDI + 1 DockDocument; save JSON; create fresh manager; load; assert each panel has the correct LayoutKind |
