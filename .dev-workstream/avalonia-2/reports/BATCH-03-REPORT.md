# BATCH-03 Report — Avalonia Port: Tests, Dock Integration, AvaloniaWindowManager

**Date**: 2025-01-31  
**Status**: ✅ APPROVED — all success criteria met

---

## Success Criteria

| Criterion | Result |
|-----------|--------|
| `dotnet build CycloneDDS.NET.sln -c Debug` → 0 errors | ✅ |
| `dotnet test tests/DdsMonitor.Avalonia.Tests/` → 0 failures | ✅ 78/78 passed |
| `dotnet test tests/DdsMonitor.Avalonia.Core.Tests/` → 0 failures | ✅ 27/27 passed |

---

## Phase 1 — Fix Fake/Incomplete Tests (C0-1..C0-7)

### C0-1..C0-4: MdiChildTests.cs
Fixed 4 incomplete tests in `tests/DdsMonitor.Avalonia.Tests/Controls/MdiChildTests.cs`:

- **C0-1** `IsActive_True_AddsActiveClass` / `IsActive_False_RemovesActiveClass` — properly assert CSS class presence via `child.Classes.Contains("active")`
- **C0-2** `CloseButton_Click_RaisesCloseRequested` — use headless pointer simulation to find and click `PART_CloseButton`
- **C0-3** `Drag_PointerMoved_RaisesDragRequested` — simulate pointer press + move on titlebar, verify `DragRequestedEvent` fires
- **C0-4** `Escape_DuringDrag_RaisesDragCancelled` — set `_mode` via reflection to `Drag`, press Escape, verify `DragCancelledEvent` fires  
  **Root cause fixed**: `MdiChild` was not focusable; added `FocusableProperty.OverrideDefaultValue<MdiChild>(true)` so keyboard events route correctly.

### C0-5..C0-7: MdiHostTests.cs
Fixed 3 incomplete tests in `tests/DdsMonitor.Avalonia.Tests/Controls/MdiHostTests.cs`:

- **C0-5** `Add_AddsChildToChildren` — verify `Children.Count == 1` after `Add()`
- **C0-6** `Remove_RemovesChildFromChildren` — verify `Children.Count == 0` after `Remove()`
- **C0-7** `BringToFront_UpdatesZIndex` — add two children, bring second to front, verify z-order

---

## Phase 2 — M1-T6: IDockManager, DdsDockFactory, DockManager + Tests

### New files

| File | Purpose |
|------|---------|
| `tools/DdsMonitor/DdsMonitor.Avalonia/Docking/IDockManager.cs` | Public interface; `DockSide` enum; `Initialise`, `AddDocument`, `AddTool`, `Remove`, `TryFocus`, `SerialiseLayout`, `DeserialiseLayout`, `DocumentClosed` |
| `tools/DdsMonitor/DdsMonitor.Avalonia/Docking/DdsDockFactory.cs` | `Factory` subclass; builds default Dock.NET layout with ProportionalDock tree, MDI workspace as embedded `Document`, left/right/bottom `ToolDock`s |
| `tools/DdsMonitor/DdsMonitor.Avalonia/Docking/DockManager.cs` | `IDockManager` implementation; manages `DockControl`, routes `AddDocument`/`AddTool` to `IDocumentDock`/`IToolDock`, serialises layout as JSON |
| `tests/DdsMonitor.Avalonia.Tests/Docking/DockManagerTests.cs` | 4 headless tests: `Initialise_SetsDocumentDock`, `AddDocument_AddsToDocumentDock`, `AddTool_AddsToToolDock`, `DocumentClosed_EventFires` |

### Namespace fix
`IRootDock` is in `Dock.Model.Controls` (not `Dock.Model.Core`). `DockBase` is in `Dock.Model.Mvvm.Core`. Added correct `using` directives to both `DdsDockFactory.cs` and `DockManager.cs`.

---

## Phase 3 — M1-T5: AvaloniaWindowManager Rewrite + IAvaloniaWindowManager + Tests

### New / rewritten files

| File | Purpose |
|------|---------|
| `tools/DdsMonitor/DdsMonitor.Avalonia/IAvaloniaWindowManager.cs` | Extended interface: `SetMdiHost`, `SetDockManager`, `SpawnPanel(string, LayoutKind, Dict?)`, `MoveToLayout` |
| `tools/DdsMonitor/DdsMonitor.Avalonia/AvaloniaWindowManager.cs` | Full rewrite; routes MDI panels via `MdiHost`, dock panels via `IDockManager`; workspace save/load via JSON; geometry persisted on close |
| `tests/DdsMonitor.Avalonia.Tests/AvaloniaWindowManagerTests.cs` | 6 headless tests in class `AvaloniaWindowManagerDockTests` |

### Key implementation decisions

**MDI close chain**: `ClosePanel()` calls `MdiHost.Remove()` → `ChildRemoved` event → `OnPanelRemoved()`. This ensures both programmatic close and user-click-close flow through the same path. `SetMdiHost()` subscribes to `ChildRemoved`.

**Geometry persistence**: `OnPanelRemoved()` always writes `ComponentState["__window"]` with `{X, Y, Width, Height}` from the current `MdiChild` dimensions (or fallback to `PanelState` values). This preserves backward compat with the existing `ShellTests.cs` geometry tests.

**Layout routing**: `SpawnPanel(string, LayoutKind, Dict?)` routes to `SpawnMdiPanel` or `SpawnDockPanel` based on `LayoutKind`. If the target host is not set, the panel is still tracked in `_activePanels` (graceful degradation).

**MoveToLayout**: Returns `NotSupportedException` — deferred to M2 per spec.

### Tests (class `AvaloniaWindowManagerDockTests`)

1. `SpawnPanel_Mdi_AddsChildToMdiHost` — MdiHost.Children.Count == 1 after spawn
2. `SpawnPanel_DockDocument_AddsToDocumentManager` — StubDockManager.AddedDocuments contains panel id
3. `ClosePanel_Mdi_RemovesFromActivePanels` — ActivePanels.Count == 0 after close
4. `ExcludedTopics_SetAndGet` — round-trip via `SetExcludedTopics` / `ExcludedTopics`
5. `SaveWorkspaceToJson_ContainsPanel` — JSON contains panel id after spawn
6. `BringToFront_Mdi_DoesNotThrow` — no exception when calling BringToFront on active panel

---

## Issues Encountered and Resolutions

| Issue | Resolution |
|-------|-----------|
| `IRootDock` not found in `DdsDockFactory.cs` | Type is in `Dock.Model.Controls`, not `Dock.Model.Core`; added correct using |
| `DockBase` not found in `DockManager.cs` | Type is in `Dock.Model.Mvvm.Core`; added correct using |
| Duplicate `AvaloniaWindowManagerTests` class | Old class exists in `ShellTests.cs`; renamed new class to `AvaloniaWindowManagerDockTests` |
| `MdiChild` not receiving keyboard events | Not focusable by default; added `FocusableProperty.OverrideDefaultValue<MdiChild>(true)` |
| `ClosePanel` not removing from `ActivePanels` | `MdiHost.Remove` fires `ChildRemoved` event; subscribed in `SetMdiHost` instead of using `MdiChild.CloseRequested` |
| `ComponentState["__window"]` missing on close | Added geometry serialisation in `OnPanelRemoved` for backward compat with `ShellTests.cs` |

---

## Deferred to M2

- `MoveToLayout(string, LayoutKind)` — moving a panel between MDI and Dock layouts at runtime
- `DdsDockFactory` XAML wiring (factory must be assigned to `DockControl.Factory` in the main shell view)
- Plugin-triggered layout changes via event broker
