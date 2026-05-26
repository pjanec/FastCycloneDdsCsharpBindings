# BATCH-03 Instructions

**Branch:** `ddsmon-avalonia`  
**Tasks:** Corrective T0 (test fixes from BATCH-02 review) + M1-T5 + M1-T6  
**Priority order:** Complete Corrective T0 first; then M1-T6; then M1-T5 (M1-T5 depends on M1-T6's IDockManager).

---

## Prerequisites — Read First

1. Read `.github/skills/developer/SKILL.md` for the developer workflow.
2. Read `.dev-workstream/avalonia-2/DESIGN.md` **end-to-end** before writing any code.
3. Read `.dev-workstream/avalonia-2/TASK-DETAILS.md` sections for M1-T5 and M1-T6 in full.
4. Read `.dev-workstream/avalonia-2/reviews/BATCH-02-REVIEW.md` for the P1/P2 issues you must fix.

---

## Corrective Task 0 — Fix Fake/Incomplete Tests from BATCH-02

These are mandatory fixes. The tests currently pass via trivial/no-op assertions.
All 7 items must be fixed before starting M1-T5/M1-T6.

### C0-1: Fix `Drag_PointerMoved_RaisesDragRequested` — FAKE (P1)

**File:** `tests/DdsMonitor.Avalonia.Tests/Controls/MdiChildTests.cs`

**Problem:** `Assert.True(dragArgs is not null || true, ...)` — the `|| true` makes this always pass.

**Fix:** After `window.Show()`, the `MdiChild` (400×300) is the window's `Content` and occupies position (0, 0). The titlebar is `PART_Titlebar` at y=[0..28]. Simulate a left-button press at (10, 10), move to (60, 10), and assert:
```csharp
Assert.NotNull(dragArgs);
```
If headless mouse events do not propagate through to the handler (because `_mode` was never set to `Drag` due to pointer-capture limitations), use this alternative approach instead:
- Get the titlebar via `GetVisualDescendants().OfType<Border>().First(b => b.Name == "PART_Titlebar")`
- Manually raise a `PointerPressedEventArgs` on the titlebar to set drag mode
- Then manually raise `PointerEventArgs` (PointerMoved) on the titlebar
- Assert `dragArgs != null`

If neither approach works in headless, the correct fallback is to test at a lower level: invoke the private-level event wiring by subscribing to the routed event directly on the child, then use reflection to call `OnTitlebarPointerMoved` with a fabricated `PointerEventArgs` after forcing `_mode = Drag` via reflection. This is acceptable in headless tests where true pointer simulation is unreliable.

> **Important:** Whatever approach you take, the assertion MUST be `Assert.NotNull(dragArgs)` — not `Assert.True(... || true)`. The test must be able to fail.

### C0-2: Fix `Resize_BottomRight_RaisesResizeRequested` — FAKE (P1)

**File:** `tests/DdsMonitor.Avalonia.Tests/Controls/MdiChildTests.cs`

**Problem:** The test only checks `handle != null` (template structure). `resizeArgs` is never subscribed to or asserted.

**Fix:**
```csharp
MdiChildResizeEventArgs? resizeArgs = null;
child.AddHandler(MdiChild.ResizeRequestedEvent, (s, e) => resizeArgs = (MdiChildResizeEventArgs)e);
```
Then simulate pointer events on the `PART_ResizeBottomRight` handle OR use the same approach as C0-1 (raise events on the handle directly or use reflection to set `_mode = Resize, _resizeEdge = Bottom|Right` and then trigger `OnResizeHandleMoved`).

Assertions:
```csharp
Assert.NotNull(resizeArgs);
Assert.True(resizeArgs!.Edge.HasFlag(ResizeEdge.Bottom));
Assert.True(resizeArgs!.Edge.HasFlag(ResizeEdge.Right));
```

### C0-3: Fix `Escape_DuringDrag_RaisesDragCancelled` — FAKE (P1)

**File:** `tests/DdsMonitor.Avalonia.Tests/Controls/MdiChildTests.cs`

**Problem:** After `MouseDown + KeyPress(Escape)`, `cancelled` is never asserted.

**Fix:** Ensure the MdiChild has keyboard focus and drag mode is active before pressing Escape:
1. Call `window.MouseDown(new Point(10, 10), MouseButton.Left, RawInputModifiers.None)` on the titlebar area
2. Ensure focus: `child.Focus()` or `window.Focus()`
3. Call `window.KeyPress(Key.Escape, RawInputModifiers.None)`

Assert: `Assert.True(cancelled, "DragCancelled must fire when Escape is pressed during drag.")`

If drag mode cannot be entered via pointer simulation, use reflection to force `_mode = InteractionMode.Drag` before pressing Escape, then assert `cancelled == true`.

### C0-4: Fix `TitlebarContextMenu_HasSixMenuItems` — Weak assertion (P2)

**File:** `tests/DdsMonitor.Avalonia.Tests/Controls/MdiChildTests.cs`

**Problem:** Asserts `>= 5` but the context menu has exactly 6 `MenuItem` objects (4 dock items + Minimise + Close; the Separator is not a MenuItem).

**Fix:** Change to `Assert.Equal(6, menuItems.Count)`.

### C0-5: Fix `Minimise_HidesChild_AndAddsToStrip` — Incomplete assertion (P2)

**File:** `tests/DdsMonitor.Avalonia.Tests/Controls/MdiHostTests.cs`

**Problem:** Test only checks `child.IsVisible == false` and `child.IsMinimised == true`. Does not verify the strip received a button.

**Fix:** After `host.Minimise("m")`, locate the minimised strip via the visual tree:
```csharp
var strip = host.GetVisualDescendants()
                .OfType<ItemsControl>()
                .FirstOrDefault(c => c.Name == "PART_MinimisedStrip");
Assert.NotNull(strip);
Assert.Equal(1, strip!.Items.Count);
```

### C0-6: Fix `Restore_ReappearsAndStripIsEmpty` — Incomplete assertion (P2)

**File:** `tests/DdsMonitor.Avalonia.Tests/Controls/MdiHostTests.cs`

**Problem:** After restore, strip item count is not verified.

**Fix:** After `host.Restore("r")`, assert:
```csharp
var strip = host.GetVisualDescendants()
                .OfType<ItemsControl>()
                .FirstOrDefault(c => c.Name == "PART_MinimisedStrip");
Assert.NotNull(strip);
Assert.Equal(0, strip!.Items.Count);
```

### C0-7: Fix `FocusNext_WithThreeChildren_DoesNotThrow` — Weak assertion (P2)

**File:** `tests/DdsMonitor.Avalonia.Tests/Controls/MdiHostTests.cs`

**Problem:** Only checks no exception. Does not verify any child's active/focus state changed.

**Fix:** The simplest verifiable behavior is that `BringToFront` was called (z-order changed). After 3× `FocusNext(false)`:
- Record ZIndex of each child before calling FocusNext
- Call FocusNext once
- Assert the highest ZIndex child has changed (a different child now has the max ZIndex)

```csharp
host.TryGet("f1", out var c1); host.TryGet("f2", out var c2); host.TryGet("f3", out var c3);
var maxBefore = new[] { c1.ZIndex, c2.ZIndex, c3.ZIndex }.Max();
var idBefore = maxBefore == c1.ZIndex ? "f1" : maxBefore == c2.ZIndex ? "f2" : "f3";

host.FocusNext(false); // advance to next child

var maxAfter = new[] { c1.ZIndex, c2.ZIndex, c3.ZIndex }.Max();
var idAfter = maxAfter == c1.ZIndex ? "f1" : maxAfter == c2.ZIndex ? "f2" : "f3";

Assert.NotEqual(idBefore, idAfter); // a different child was brought to front
```

---

## M1-T6 — Dock.NET Integration

> Complete this before M1-T5. M1-T5's IDockManager parameter comes from here.

Read `TASK-DETAILS.md#m1-t6--docknet-integration` in full.

**Key files to create:**

### `tools/DdsMonitor/DdsMonitor.Avalonia/Docking/IDockManager.cs`

```csharp
using Avalonia.Controls;
using DdsMonitor.Avalonia.Controls;
using Dock.Avalonia.Controls;

namespace DdsMonitor.Avalonia.Docking;

public enum DockSide { Left, Right, Bottom }

public interface IDockManager
{
    void Initialise(DockControl dockControl, MdiHost mdiHost);
    void AddDocument(string id, string title, Control content);
    void AddTool(string id, string title, Control content, DockSide side = DockSide.Left);
    bool Remove(string id);
    bool TryFocus(string id);
    string SerialiseLayout();
    void DeserialiseLayout(string json);
    event Action<string>? DocumentClosed;
}
```

### `tools/DdsMonitor/DdsMonitor.Avalonia/Docking/DdsDockFactory.cs`

- Subclass `Dock.Model.Mvvm.Factory`.
- Override `CreateLayout()` to build the default dock tree per DESIGN.md §3.
- The central "MDI Workspace" `Document` must have `CanClose = false` and `CanFloat = false`.
- Side `ToolDock`s start collapsed/invisible (no tools yet).

### `tools/DdsMonitor/DdsMonitor.Avalonia/Docking/DockManager.cs`

- Implements `IDockManager`.
- `Initialise(dockControl, mdiHost)`:
  - Creates `DdsDockFactory`, builds layout with `CreateLayout()`.
  - Sets the MDI Workspace document content to `mdiHost`.
  - Assigns the layout to `dockControl.Layout = layout`.
  - Wires up Dock.NET close callbacks to fire `DocumentClosed`.
- `AddDocument(id, title, content)`:
  - Creates a `Document` with the given id/title/content.
  - Adds it to the `DocumentDock` next to the MDI Workspace document.
- `AddTool(id, title, content, side)`:
  - Creates a `Tool` in the matching `ToolDock`.
  - Makes the `ToolDock` visible.
- `SerialiseLayout()`: Use `System.Text.Json` on a DTO capturing documents and tools (id, title, side for tools).
- `DeserialiseLayout(json)`: Restore the tools/documents from DTO (do not restore the MDI Workspace — it is always present).

### `tests/DdsMonitor.Avalonia.Tests/Docking/DockManagerTests.cs`

Four `[AvaloniaFact]` tests:

1. `AddDocument_CreatesDocumentTab` — after `AddDocument("a", "A", ...)`, verify the dock layout has 2 documents in the DocumentDock (MDI Workspace + new).
2. `AddTool_MakesLeftToolDockVisible` — after `AddTool("t", "T", ..., Left)`, the left ToolDock is visible/non-collapsed.
3. `Remove_DocumentFiresDocumentClosed` — `Remove("a")` fires `DocumentClosed("a")` event.
4. `SerialiseLayout_RoundTrip` — `SerialiseLayout()` then `DeserialiseLayout(saved)` restores the same document/tool IDs.

**Implementation notes:**
- If Dock.NET 11.2 does not have a `DockControl` that can be used in headless tests, instantiate `DockManager` directly (without `DockControl`) and test the factory/layout tree in isolation.
- Check Dock.NET's actual NuGet API for the correct class names: `RootDock`, `ProportionalDock`, `ToolDock`, `DocumentDock`, `Document`, `Tool` are the expected names from Dock.Model.Mvvm. Verify by inspecting the package.
- `DockFactory` in Dock.Model.Mvvm is named `Factory` — `DdsDockFactory` extends it.

---

## M1-T5 — New AvaloniaWindowManager

Read `TASK-DETAILS.md#m1-t5--new-avaloniawindowmanager` in full.

**Key context: what the current code does**

`tools/DdsMonitor/DdsMonitor.Avalonia/AvaloniaWindowManager.cs` currently opens each panel in its own standalone `Window` (floating OS-level windows). M1-T5 replaces this with MDI/Dock placement.

**Key changes:**

### New interface `IAvaloniaWindowManager`

Create `tools/DdsMonitor/DdsMonitor.Avalonia/IAvaloniaWindowManager.cs`:

```csharp
using DdsMonitor.Avalonia.Controls;
using DdsMonitor.Engine;

namespace DdsMonitor.Avalonia;

public interface IAvaloniaWindowManager : IWindowManager
{
    PanelState SpawnPanel(string componentTypeName, LayoutKind layout,
                          Dictionary<string, object>? initialState = null);
    void MoveToLayout(string panelId, LayoutKind newKind);
    void SetMdiHost(MdiHost host);
    void SetDockManager(IDockManager dockManager);
}
```

Note: `LayoutKind` is in `DdsMonitor.Avalonia.Core` namespace. Import it.

### Rewrite `AvaloniaWindowManager.cs`

Replace the existing class to implement `IAvaloniaWindowManager`.

**Key behavior changes from the existing implementation:**

- Remove `_openWindows` (`Dictionary<string, Window>`) — panels no longer live in standalone OS windows.
- Add `_mdiHost: MdiHost?` (set via `SetMdiHost`) and `_dockManager: IDockManager?` (set via `SetDockManager`).
- The original `SpawnPanel(componentTypeName, initialState?)` (interface method) delegates to `SpawnPanel(componentTypeName, LayoutKind.Mdi, initialState)`.
- `SpawnPanel(name, LayoutKind.Mdi, state)`:
  - Build VM + view using the same logic as the current `OpenPanelWindow`.
  - Create `MdiChild { ChildId = panelId, Title = panelState.Title, Content = view }`.
  - Read geometry from `ComponentState["__window"]` (X, Y, Width, Height, IsMinimised).
  - Call `_mdiHost?.Add(child, x, y, width, height)` on the UI thread.
  - Track child in `_mdiChildren: Dictionary<string, MdiChild>`.
- `SpawnPanel(name, LayoutKind.DockDocument, state)`:
  - Build VM + view.
  - Call `_dockManager?.AddDocument(panelId, title, view)` on the UI thread.
  - Track in `_dockItems`.
- `SpawnPanel(name, LayoutKind.DockTool, state)`:
  - Build VM + view.
  - Read `ComponentState["__tool_side"]` for DockSide (default Left).
  - Call `_dockManager?.AddTool(panelId, title, view, side)`.
- `MoveToLayout(panelId, newKind)`:
  - Determine current placement; remove from current host.
  - Re-add to new host with the same VM/view.
  - Update `panelState.LayoutKind` (add `LayoutKind` field to `PanelState` in engine? — **No**: do NOT modify engine types; track `LayoutKind` per-panel in the window manager's own dictionary).
- `ClosePanel(panelId)`:
  - Determine placement; remove from MDI host or dock manager.
  - Dispose VM if IDisposable.
  - Fire `PanelClosed`; publish `WorkspaceSaveRequestedEvent`.
- `SaveWorkspaceToJson()`:
  - Walk MDI children (Canvas.GetLeft/Top for geometry), dock documents, dock tools.
  - Add `LayoutKind` per panel (using the manager's layout tracking dictionary).
  - Capture `IsMinimised` from `MdiChild.IsMinimised`.
  - Add `DockLayout` field from `_dockManager?.SerialiseLayout()`.
  - Preserve `ExcludedTopics` and `PluginSettings` exactly.
- `LoadWorkspaceFromJson(json)`:
  - Parse. Restore `ExcludedTopics` first.
  - For each panel entry, read `LayoutKind` field (string, default `"Mdi"` if absent — Blazor compat).
  - Call `SpawnPanel(name, parsedKind, state)`.
  - Restore dock layout via `_dockManager?.DeserialiseLayout(json)` if present.
  - Publish `WorkspaceLoadedEvent`.
- `BringToFront(panelId)`:
  - If MDI: `_mdiHost?.BringToFront(panelId)`.
  - If Dock: `_dockManager?.TryFocus(panelId)`.
- `ShowPanel(panelId)`:
  - If MDI: `_mdiHost?.Restore(panelId)`.
  - Else: `BringToFront(panelId)`.

### Tests — `AvaloniaWindowManagerTests.cs`

Create `tests/DdsMonitor.Avalonia.Tests/AvaloniaWindowManagerTests.cs`.

Minimum 6 `[AvaloniaFact]` tests:

1. `SpawnPanel_Mdi_AddsChildToMdiHost` — spawn with `LayoutKind.Mdi`, assert `host.Children.Count == 1`.
2. `SpawnPanel_DockDocument_CallsAddDocument` — spawn with `LayoutKind.DockDocument`, assert fake `IDockManager.AddDocument` was called with correct id/title.
3. `MoveToLayout_MdiToDockDocument_RemovesFromMdiAndAddsToDoc` — spawn MDI, then `MoveToLayout(id, DockDocument)`, assert MDI count = 0 and dock AddDocument called; assert same VM instance reused.
4. `ClosePanel_DisposesVm` — use a test VM with `IDisposable`; close panel, assert disposed.
5. `SaveAndLoad_Blazor_CompatWorkspace_AllMdi` — load a JSON workspace that has no `LayoutKind` field (Blazor format), assert all panels spawned with `LayoutKind.Mdi`.
6. `SaveWorkspaceToJson_RoundTrips_LayoutKind` — spawn 1 MDI + 1 DockDocument panel; save; load fresh manager; assert correct LayoutKinds restored.

**Implementation guidance:**

- Use a fake `IDockManager` (simple mock class with tracking fields) in tests.
- Use a real `MdiHost` in a headless `Window` for MDI tests.
- The `AvaloniaWindowManager` must not reference `MdiHost` if `SetMdiHost` was not called — all MDI operations must null-check `_mdiHost` and log a warning (but not throw) if it is null.
- Threading: Wrap all UI mutations in `Dispatcher.UIThread.Post(...)` (same as existing code). In tests, use `await Dispatcher.UIThread.InvokeAsync(...)` with `AvaloniaFact` instead.

---

## Validation Steps

After completing all tasks, run:

```powershell
cd d:\Work\FastCycloneDdsCsharpBindings
dotnet build CycloneDDS.NET.sln -c Debug
dotnet test tests/DdsMonitor.Avalonia.Tests/ -c Debug
dotnet test tests/DdsMonitor.Avalonia.Core.Tests/ -c Debug
```

All three must succeed with **0 errors** and **0 failing tests**.

---

## Report

Write your completion report to `.dev-workstream/avalonia-2/reports/BATCH-03-REPORT.md`.

The report must answer these developer insight questions:

1. **C0-1/C0-2/C0-3**: Which approach did you use for pointer event simulation — headless window events, direct routed event wiring on template parts, or reflection? Did all 3 P1 tests now have real assertions that can fail?
2. **C0-5/C0-6**: Was `PART_MinimisedStrip` findable in the visual tree after `Minimise()` was called? What is its item count after minimise vs restore?
3. **IDockManager API**: Which Dock.NET 11.2 types did you use from the package? How does `DdsDockFactory.CreateLayout()` wire up the `MdiHost` document?
4. **AvaloniaWindowManager threading**: How did you handle `SpawnPanel` being called from a non-UI thread vs the UI-thread requirement for `_mdiHost.Add`?
5. **Round-trip test**: Does the Blazor-format workspace (no `LayoutKind`, no `DockLayout`) correctly restore all panels as `LayoutKind.Mdi`?
6. **Weak points**: Any tests you were unable to make fully behavioral? Note them with justification.
