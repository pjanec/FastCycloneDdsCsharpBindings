# Task Details

Each task below is a self-contained unit of work for a coding agent. Read
`DESIGN.md` end-to-end before starting any task. Each task lists:

- **Goal**: one-sentence objective.
- **Depends on**: tasks that must be complete first.
- **Inputs to read**: source files, sections of DESIGN.md.
- **Deliverables**: exact files to create or modify.
- **Implementation notes**: non-obvious decisions.
- **Acceptance criteria**: testable conditions. The task is **done** when
  *all* criteria pass.
- **Out of scope**: things the agent must not do (to avoid drift).

When a task's acceptance criteria all pass, tick its box in
`TASK-TRACKER.md` and stop. Do not continue into the next task — the user
reviews each task's output and may amend the plan.

If a criterion cannot be met as written, **stop and ask** rather than improvising.

---

## M1-T0 — Preflight: Solution Skeleton and Shared Assemblies

**Goal**: Add Dock.NET, register `DdsMonitor.Avalonia.Core` as a plugin-shared
assembly, create the `DdsMonitor.Avalonia.FeatureDemoPlugin` project skeleton,
and add the test project entries.

**Depends on**: nothing.

**Inputs to read**:

- `DESIGN.md` §2 (Solution Layout), §10 (Plugin Loading and Asset Staging).
- Existing `tools/DdsMonitor/DdsMonitor.Avalonia.StandardPlugin/DdsMonitor.Avalonia.StandardPlugin.csproj`
  (template for new plugin projects, especially the `StagePlugin` MSBuild target).
- Existing `tools/DdsMonitor/DdsMonitor.Engine/Plugins/PluginLoader.cs`
  (the `SharedAssemblyNames` HashSet).
- Existing `CycloneDDS.NET.sln`.

**Deliverables**:

1. **Modify** `tools/DdsMonitor/DdsMonitor.Avalonia/DdsMonitor.Avalonia.csproj`:
   - Add `<PackageReference Include="Dock.Avalonia" Version="11.2.0" />`
     (use the latest 11.2.x compatible with Avalonia 11.2.3).
   - Add `<PackageReference Include="Dock.Model.Mvvm" Version="11.2.0" />`.
   - Add a `ProjectReference` to the new
     `DdsMonitor.Avalonia.FeatureDemoPlugin.csproj` with
     `ReferenceOutputAssembly="false"` `Private="false"`
     `SkipGetTargetFrameworkProperties="true"`, mirroring the existing
     StandardPlugin reference.

2. **Modify** `tools/DdsMonitor/DdsMonitor.Engine/Plugins/PluginLoader.cs`:
   - Confirm the entries `"DdsMonitor.Engine"` and
     `"DdsMonitor.Avalonia.Core"` are present in `SharedAssemblyNames`. If
     `"DdsMonitor.Avalonia.Core"` is missing, add it. Do not remove any
     existing entries.

3. **Create** `tools/DdsMonitor/DdsMonitor.Avalonia.FeatureDemoPlugin/DdsMonitor.Avalonia.FeatureDemoPlugin.csproj`:
   - Mirror `DdsMonitor.Avalonia.StandardPlugin.csproj`. Same `TargetFramework`
     (`net8.0`), same `Nullable`/`ImplicitUsings`/`LangVersion` settings.
   - Include the same `StagePlugin` MSBuild target verbatim (the one that
     copies the DLL into the shell's `plugins/` folder).
   - Reference `DdsMonitor.Engine`, `DdsMonitor.Avalonia.Core`, plus `Avalonia`
     and `Microsoft.Extensions.Hosting.Abstractions` packages (same versions
     as StandardPlugin).
   - Add `InternalsVisibleTo` for `DdsMonitor.Avalonia.FeatureDemoPlugin.Tests`.

4. **Create** `tools/DdsMonitor/DdsMonitor.Avalonia.FeatureDemoPlugin/Placeholder.cs`:
   - A single internal placeholder class `internal static class Placeholder { }`
     so the project compiles. Will be deleted in M1-T10.

5. **Modify** `CycloneDDS.NET.sln`:
   - Add the new `DdsMonitor.Avalonia.FeatureDemoPlugin` project to the
     solution under the `DdsMonitor` solution folder, with both `Debug` and
     `Release` build configurations enabled for `Any CPU`.

6. **Create test project skeletons** (empty xUnit projects, one class file each):

   - `tests/DdsMonitor.Avalonia.Tests/DdsMonitor.Avalonia.Tests.csproj` (if missing)
   - `tests/DdsMonitor.Avalonia.Core.Tests/DdsMonitor.Avalonia.Core.Tests.csproj`
   - `tests/DdsMonitor.Avalonia.StandardPlugin.Tests/DdsMonitor.Avalonia.StandardPlugin.Tests.csproj` (if missing)
   - `tests/DdsMonitor.Avalonia.FeatureDemoPlugin.Tests/DdsMonitor.Avalonia.FeatureDemoPlugin.Tests.csproj`

   Each references xUnit (2.9.x), `xunit.runner.visualstudio`,
   `Microsoft.NET.Test.Sdk` (latest 17.x), and the corresponding project
   under test. The `DdsMonitor.Avalonia.Tests` project also references
   `Avalonia.Headless.XUnit` (matching Avalonia version).

   Each test project gets a single `Placeholder.cs` containing
   `public class PlaceholderTests { [Fact] public void True_is_true() => Assert.True(true); }`.

   Register all four test projects in the solution under a `tests` folder.

**Implementation notes**:

- If `Dock.Avalonia 11.2.0` does not exist, use the highest 11.2.x available;
  document the exact version chosen in a comment in the csproj.
- The `StagePlugin` MSBuild target's `PluginsDir` path is relative; ensure the
  new FeatureDemoPlugin csproj uses the same path scheme.

**Acceptance criteria**:

- [x] `dotnet build CycloneDDS.NET.sln -c Debug` succeeds with **zero
  errors**. Warnings are tolerated but should be noted.
- [x] `dotnet test CycloneDDS.NET.sln -c Debug` runs and reports
  4 passing placeholder tests, one per new test project.
- [x] `DdsMonitor.Avalonia.Core` is present in
  `PluginLoader.SharedAssemblyNames`.
- [x] `Dock.Avalonia` and `Dock.Model.Mvvm` package references appear in
  the Avalonia shell csproj.
- [x] After a Debug build, the file
  `tools/DdsMonitor/DdsMonitor.Avalonia/bin/Debug/net8.0/plugins/DdsMonitor.Avalonia.FeatureDemoPlugin.dll`
  exists (proving the StagePlugin target works).
- [x] The solution file lists both new plugin and all four test projects
  under the correct solution folders.

**Out of scope**:

- Any actual plugin or test code beyond placeholders.
- Modifying the existing FeatureDemo plugin under `DdsMonitor.Plugins.FeatureDemo`
  (that's the legacy Blazor one — leave it).

---

## M1-T1 — Avalonia.Core Abstractions

**Goal**: Add the new types and service interfaces to
`DdsMonitor.Avalonia.Core` that subsequent tasks depend on: `LayoutKind`,
updated `ToolbarEntry`, service interfaces (`IContextMenuPresenter`,
`IFileDialogService`, `IKeyboardShortcutService`, `IThemeService`,
`IClipboardService`, `IUiThreadInvoker`).

**Depends on**: M1-T0.

**Inputs to read**:

- `DESIGN.md` §9 (Services and Registries), §12 (Coding Conventions).
- Existing `DdsMonitor.Avalonia.Core/ToolbarEntry.cs`,
  `IToolbarRegistry.cs`, `IUserSettings.cs`,
  `IAvaloniaViewRegistry.cs`, `IAvaloniaTypeDrawerRegistry.cs`,
  `IEventBrokerExtensions.cs`, `IStatefulViewModel.cs`.
- Engine's `IContextMenuRegistry.cs` and `ContextMenuItem.cs` (to understand
  what `IContextMenuPresenter` adapts).
- DESIGN.md §13 (Known Bugs) item 6: `ToolbarEntry` extension.

**Deliverables**:

All under `tools/DdsMonitor/DdsMonitor.Avalonia.Core/`. New files unless
marked "modify".

1. **Create** `LayoutKind.cs` — enum from DESIGN.md §3.

2. **Modify** `ToolbarEntry.cs`:
   - Convert from positional record to record with `Label` field added.
   - Order: `Id`, `Action`, `Label`, `Tooltip`, `IconKey?`. `Label` is the
     short visible button text (may be empty if `IconKey` is set).
   - Update consumers if any compile errors arise from the order change
     (only the existing `ToolbarRegistry.Register` overload — extend it to
     accept `label`).

3. **Modify** `IToolbarRegistry.cs`:
   - Change `Register(string id, Action onClick, string? iconKey = null, string tooltip = "")`
     to
     `Register(string id, Action onClick, string label = "", string tooltip = "", string? iconKey = null)`.
   - Update `ToolbarRegistry.cs` implementation to match.

4. **Create** `IContextMenuPresenter.cs`:
   - Method
     `void Show(Control anchor, object dataContext, IReadOnlyList<DdsMonitor.Engine.Plugins.ContextMenuItem> defaultItems = null)`.
   - Responsibility: combine `defaultItems` (caller-provided) with items from
     `IContextMenuRegistry.GetItems(dataContext)`, build an Avalonia
     `ContextMenu`, attach it to `anchor`, and open it at the current
     pointer position. If the menu is already attached, just open it.

5. **Create** `IFileDialogService.cs`:
   - Async methods:
     `Task<string?> OpenFileAsync(string title, IReadOnlyList<FilePickerFilter> filters, string? initialDirectory = null)`
     and
     `Task<string?> SaveFileAsync(string title, string suggestedName, IReadOnlyList<FilePickerFilter> filters)`.
   - `FilePickerFilter` is a co-located public record:
     `record FilePickerFilter(string Name, IReadOnlyList<string> Extensions)`.

6. **Create** `IKeyboardShortcutService.cs`:
   - Methods:
     `void Register(KeyGesture gesture, string description, Action action)`,
     `IReadOnlyList<RegisteredShortcut> Registered { get; }`,
     `bool TryInvoke(KeyGesture gesture)`.
   - `RegisteredShortcut` is a co-located public record:
     `record RegisteredShortcut(KeyGesture Gesture, string Description, Action Action)`.
   - Note: `KeyGesture` is `Avalonia.Input.KeyGesture`; this is fine for
     Avalonia.Core (which already references Avalonia).

7. **Create** `IThemeService.cs`:
   - Enum `ThemeMode { System, Light, Dark }`.
   - Property `ThemeMode CurrentMode { get; }`.
   - Event `event Action<ThemeMode>? ModeChanged`.
   - Method `void SetMode(ThemeMode mode)`.
   - The setting is persisted via `IUserSettings` under section `Theme` key
     `Mode`. The service caches the mode in memory.

8. **Create** `IClipboardService.cs`:
   - Methods `Task SetTextAsync(string text)` and `Task<string?> GetTextAsync()`.

9. **Create** `IUiThreadInvoker.cs`:
   - Interface used by ViewModels in tests to substitute `Dispatcher.UIThread`.
   - Methods: `bool CheckAccess()`, `void Post(Action action)`,
     `Task InvokeAsync(Func<Task> action)`.
   - Production implementation `AvaloniaUiThreadInvoker` (also in this
     file) wraps `Dispatcher.UIThread`.

10. **Update** `IEventBrokerExtensions.cs`:
    - Add an overload
      `SubscribeOnUiThread<TEvent>(this IEventBroker broker, Action<TEvent> handler, IUiThreadInvoker invoker)`
      that takes the abstraction instead of the concrete `Dispatcher`.
    - Keep the existing `Dispatcher?` overload for backward compatibility.

11. **Create** unit tests under `tests/DdsMonitor.Avalonia.Core.Tests/`:
    - `ToolbarRegistryTests.cs`: registering and re-registering the same id
      replaces; `Changed` fires; `Entries` is a thread-safe snapshot.
    - `UserSettingsStoreTests.cs`: set then get returns the value; save then
      re-load round-trips; debounced save (use a short
      debounce override).
    - `IUiThreadInvokerTests.cs`: a test invoker implementation that runs
      synchronously and a verification that posted actions execute.
    - `AvaloniaViewRegistryTests.cs`: register VM type, build returns the
      control; unregistered type throws `InvalidOperationException`.

**Implementation notes**:

- `IContextMenuPresenter` is an interface only in this task; the
  implementation lives in M1-T8.
- All new interfaces have XML doc comments per §12 conventions.
- Be careful with `ToolbarEntry` field reorder — search for all call sites
  in StandardPlugin (`registry.Register(...)`) and update them.

**Acceptance criteria**:

- [x] `dotnet build` succeeds with zero errors.
- [x] `dotnet test tests/DdsMonitor.Avalonia.Core.Tests/` passes; all four
  new test files report ≥ 1 passing test each.
- [x] `IContextMenuPresenter`, `IFileDialogService`, `IKeyboardShortcutService`,
  `IThemeService`, `IClipboardService`, `IUiThreadInvoker`, `LayoutKind`
  exist with the signatures above.
- [x] `ToolbarEntry` has a `Label` field. All existing call sites
  (`StandardDrawerRegistrar` does **not** register toolbar entries — but
  `TopicExplorerPlugin`, `SendSamplePlugin`, etc. do; verify they compile
  with the new signature).
- [x] No file under `Avalonia.Core/` references Dock.NET, the shell project,
  or any plugin project.

**Out of scope**:

- Implementations of the new service interfaces (M1-T8).
- Wiring services into DI (M1-T9).

---

## M1-T2 — Design Tokens and Base Styles

**Goal**: Set up the resource dictionaries, brushes, and base control styles
that all panels reference.

**Depends on**: M1-T1.

**Inputs to read**:

- `DESIGN.md` §7 (Visual Design Tokens) — token table is authoritative.
- Existing `App.axaml` to know what's there.
- Avalonia 11 `FluentTheme` resource keys (for overriding) — see the
  `Avalonia.Themes.Fluent` source.

**Deliverables**:

1. **Create** `tools/DdsMonitor/DdsMonitor.Avalonia/Themes/DesignTokens.axaml`:
   - A `ResourceDictionary` with `ThemeDictionaries` for `Light` and `Dark`.
   - Every token from DESIGN.md §7 declared as a `SolidColorBrush` with the
     correct light/dark hex from the table.
   - Numeric tokens (`Spacing.Sm = 4.0`, `Spacing.Md = 8.0`, etc.) declared
     as `<x:Double x:Key="Spacing.Sm">4</x:Double>`.
   - Font tokens: `FontFamily.Body`, `FontFamily.Mono`, `FontSize.Body`,
     `FontSize.Caption`, `FontSize.Mono`.

2. **Create** `tools/DdsMonitor/DdsMonitor.Avalonia/Themes/BaseStyles.axaml`:
   - A `Styles` resource that overrides FluentTheme defaults for:
     - `Button` — flat 1 px border using `Border.Subtle`, 2 px corner radius.
     - `TextBox` — flat 1 px border, focus uses `Border.Accent`.
     - `Menu`/`MenuItem` — no icons column, smaller padding.
     - `Window` — uses `Surface.Background`.
   - Each override is conservative: only the properties that differ from
     FluentTheme defaults.

3. **Modify** `App.axaml`:
   - Replace contents with:
     - `<FluentTheme />` as the first style.
     - `<StyleInclude Source="avares://DdsMonitor.Avalonia/Themes/BaseStyles.axaml" />` as the second.
     - `<ResourceInclude Source="avares://DdsMonitor.Avalonia/Themes/DesignTokens.axaml" />` in `Application.Resources.MergedDictionaries`.

4. **Create** a visual sanity test:
   `tests/DdsMonitor.Avalonia.Tests/DesignTokensTests.cs`:
   - Headless test that constructs an `App`, looks up the brush
     `Surface.Background` in the light theme, asserts it equals
     `#FFFAFAFA`.
   - Same for `Surface.Background` in the dark theme equals `#FF1A1A1A`.
   - Test passes by exercising `Application.Current.TryFindResource("Surface.Background", out var brush)`.

**Implementation notes**:

- Use `{DynamicResource Surface.Background}` (not `StaticResource`) so theme
  switching at runtime updates bindings.
- Avalonia 11 syntax for theme dictionaries:
  ```xml
  <ResourceDictionary.ThemeDictionaries>
    <ResourceDictionary x:Key="Light">
      <SolidColorBrush x:Key="Surface.Background" Color="#FAFAFA"/>
    </ResourceDictionary>
    <ResourceDictionary x:Key="Dark">
      <SolidColorBrush x:Key="Surface.Background" Color="#1A1A1A"/>
    </ResourceDictionary>
  </ResourceDictionary.ThemeDictionaries>
  ```

**Acceptance criteria**:

- [x] `dotnet build` succeeds with zero errors.
- [x] The `DesignTokens.axaml` defines all colour, spacing, and typography
  tokens listed in DESIGN.md §7.
- [x] `DesignTokensTests` passes in both light- and dark-mode lookups.
- [x] `App.axaml` includes both `BaseStyles.axaml` and `DesignTokens.axaml`.

**Out of scope**:

- Theme-switching UI (handled by `IThemeService` impl in M1-T8).
- Per-panel styles (added by their owning milestones).

---

## M1-T3 — MdiChild Custom Control

**Goal**: Build the `MdiChild` templated control that is the floating child
window inside the MDI host.

**Depends on**: M1-T1, M1-T2.

**Inputs to read**:

- `DESIGN.md` §4 (Custom MdiHost) — especially the **MdiChild responsibilities**
  list, which is the authoritative behaviour spec.
- `DESIGN.md` §8 (Keyboard Navigation) for tab order and shortcut rules.
- Reference: existing `Components/Desktop.razor` from the dump for the drag
  and resize math (lines around `BeginDrag`/`BeginResize`/`ApplyResize`).
- Avalonia 11 `TemplatedControl` and `ContentControl` patterns (any
  reference Avalonia control's source — e.g. `Expander`).

**Deliverables**:

All under `tools/DdsMonitor/DdsMonitor.Avalonia/Controls/`.

1. **Create** `MdiChild.cs`:
   - Subclass of `ContentControl`.
   - Styled properties:
     - `Title` (string)
     - `IsActive` (bool, default false; set by host when focus is within)
     - `IsMinimised` (bool, default false; two-way)
     - `TitlebarExtras` (object, default null; right-aligned content slot)
     - `ChildId` (string; opaque identifier the host uses for tracking)
   - Direct events:
     - `RoutedEvent<RoutedEventArgs> CloseRequested`
     - `RoutedEvent<RoutedEventArgs> MinimiseRequested`
     - `RoutedEvent<MdiChildLayoutKindEventArgs> LayoutKindRequested` —
       fired when the user picks "Dock as tab" / "Dock as left tool" etc.
   - Public methods:
     - `void BringToFront()` — fires `BringToFrontRequested`.
   - `MdiChildLayoutKindEventArgs` co-located:
     `class MdiChildLayoutKindEventArgs : RoutedEventArgs { public LayoutKind TargetKind { get; init; } }`.

2. **Create** `MdiChild.axaml` (default style):
   - `ControlTemplate` with this structure:
     - Outer `Border` with `BorderBrush={DynamicResource Border.Subtle}` (or
       `Border.Accent` when `IsActive`), `BorderThickness="1"`,
       `Background={DynamicResource Surface.Panel}`.
     - Inside: `Grid RowDefinitions="28,*"`. Row 0 = titlebar, Row 1 = body.
     - Titlebar `Border` background `Surface.Titlebar` (or `TitlebarActive`
       when `IsActive`), with a horizontal `DockPanel` containing:
       - `TextBlock` showing `Title`, padded 8 px.
       - Right-docked `StackPanel` with `TitlebarExtras` ContentPresenter,
         then two `Button`s: `_` (MinimiseRequested) and `✕` (CloseRequested).
     - Body `ContentPresenter`.
     - **Eight resize handles** as `Border` controls with `Tag` set to one of
       `Top`, `Right`, `Bottom`, `Left`, `TopLeft`, `TopRight`, `BottomLeft`,
       `BottomRight`. Hit-test transparent except on hover. Cursor changes
       to the matching `StandardCursorType` (`SizeNorthSouth` etc.). Each
       handle is `Background="Transparent"` so it captures input.
   - Style selectors `:focus-within`, `:pointerover` for visual feedback.

3. **Inside `MdiChild.cs`**, wire the interactions:
   - On titlebar `PointerPressed` with left button: start a drag operation.
     Capture pointer. On `PointerMoved`, raise a private
     `DragRequested` event with the delta — but the **host** consumes that
     event and updates the child's `Canvas.Left/Top`. (Why: hit-testing
     and clamp logic belong with the host.)
   - Hold `Shift` during drag → snap movement to 8 px grid.
   - On resize handle `PointerPressed`: start resize. Same pattern — child
     fires `ResizeRequested(edge, delta)`, host applies.
   - `Escape` key while dragging/resizing cancels the operation and reverts
     position/size to the start values.
   - Right-click on the titlebar opens a `ContextMenu` with items:
     - "_Dock as tab"
     - "Dock as _left tool"
     - "Dock as _right tool"
     - "Dock as _bottom tool"
     - Separator
     - "Mi_nimise" (toggle)
     - "_Close"
     Each dock-as item fires `LayoutKindRequested` with the matching
     `LayoutKind` (the three dock-as-tool items all use `DockTool` and pass
     a side hint via the args).
   - When `Content`'s data context implements `IDisposable`, `MdiChild`
     does **not** dispose it — the host does, on close confirmation. This
     keeps cancellable closing possible.

4. **Create** `tests/DdsMonitor.Avalonia.Tests/Controls/MdiChildTests.cs`:
   - Headless tests covering:
     - Setting `IsActive` updates a visual state (verify via
       `Classes.Contains("active")` after the style trigger sets the class).
     - Clicking the close button raises `CloseRequested`.
     - Clicking the minimise button raises `MinimiseRequested`.
     - Right-click on the titlebar opens a ContextMenu with the six items.
     - Drag delta math: programmatically press, move 50 px right, release →
       a `DragRequested` event is raised with delta (50, 0).
     - Resize from `BottomRight`: press, move (100, 50), release → a
       `ResizeRequested(BottomRight, 100, 50)` is raised.
     - Escape during a drag: a `DragCancelled` event is raised.

**Implementation notes**:

- For the drag/resize event hand-off, declare two `RoutedEvent`s on the
  control: `DragRequested(double dx, double dy)` and
  `ResizeRequested(ResizeEdge edge, double dx, double dy)`. The host
  subscribes when it adds the child.
- `ResizeEdge` is a co-located flags enum:
  `[Flags] enum ResizeEdge { None = 0, Top = 1, Right = 2, Bottom = 4, Left = 8 }`.
  Corners are bitwise OR (e.g. `Top | Right` = TopRight).
- For tab order: titlebar buttons are `IsTabStop="False"`; only the
  content presenter and resize handles participate. Resize handles are
  also `IsTabStop="False"` (keyboard resize is not in M1).
- Use `Avalonia.Input.Cursors` for the cursor types.

**Acceptance criteria**:

- [x] `dotnet build` succeeds with zero errors.
- [x] All `MdiChildTests` pass.
- [x] No reference to Dock.NET in `MdiChild.cs` or `MdiChild.axaml`.
- [x] The default style renders correctly when an `MdiChild` is placed in
  an empty `Window` test fixture (headless render snapshot — manual
  verification at this milestone is OK; just ensure the test setup
  composes one MdiChild without throwing).

**Out of scope**:

- Actually positioning the child (the host does that).
- Persisting position/size (the host's responsibility).
- Animations (deferred).

---

## M1-T4 — MdiHost Custom Control

**Goal**: Build the canvas-based host that manages multiple `MdiChild`
instances, z-order, minimised strip, and boundary clamping.

**Depends on**: M1-T3.

**Inputs to read**:

- `DESIGN.md` §4 (Custom MdiHost) — full section.
- `DESIGN.md` §6 (Threading) for dispatcher behaviour.
- Reference: `Components/Desktop.razor` — `EnsurePanelsVisibleAsync`,
  `ApplyResize`, `BringToFront`, `LoadWorkspace` (for the boundary clamp
  and z-order logic).

**Deliverables**:

1. **Create** `tools/DdsMonitor/DdsMonitor.Avalonia/Controls/MdiHost.cs`:
   - Subclass of `Control` (or `TemplatedControl`).
   - Public API:
     - `void Add(MdiChild child, double x, double y, double width, double height)` —
       adds, brings to front, raises `ChildAdded`.
     - `bool Remove(string childId)` — removes by id, raises `ChildRemoved`.
     - `bool TryGet(string childId, out MdiChild child)` — convenience lookup.
     - `IReadOnlyList<MdiChild> Children { get; }` — snapshot, top-of-z-order last.
     - `void BringToFront(string childId)` — sets `Canvas.ZIndex` to max+1.
     - `void Minimise(string childId)` — hides from canvas, adds to strip.
     - `void Restore(string childId)` — reverses.
     - `void FocusNext(bool reverse)` — used by `Ctrl+Tab` cycling.
     - Events: `ChildAdded`, `ChildRemoved`, `ChildGeometryChanged(string childId, Rect newBounds, bool isMinimised)`.

2. **Create** `tools/DdsMonitor/DdsMonitor.Avalonia/Controls/MdiHost.axaml`:
   - Template containing a `DockPanel`:
     - `DockPanel.Dock="Bottom"`: the minimised strip — an `ItemsControl`
       with horizontal panel template, visible only when at least one child
       is minimised. Background `Surface.Titlebar`.
     - Centre: a `Canvas` named `PART_ChildCanvas` (no items source — the
       host adds `MdiChild`s manually via `Add()`).
   - Background `Surface.Background`.

3. **Implementation** of geometry / behaviour:
   - Subscribe to each added child's `DragRequested`, `ResizeRequested`,
     `CloseRequested`, `MinimiseRequested`, `LayoutKindRequested`,
     `BringToFrontRequested` events.
   - Drag: clamp position so titlebar stays ≥ 40 px inside host bounds.
   - Resize: enforce min width 220, min height 140.
   - Z-order: track an integer counter; `BringToFront` sets ZIndex to
     `Interlocked.Increment(ref _zCounter)`.
   - Minimise: hide the child (`IsVisible = false`) and add a `Button`
     labelled with the child's `Title` to the minimised strip. Click
     restores.
   - Clamp on host size change: subscribe to `LayoutUpdated`; when host
     bounds shrink, walk children and move them in by `max(0, child.Right - host.Width + 40)`.
   - The host **does not own** child closing logic beyond removing from
     its collection — it raises `ChildRemoved` and the `IWindowManager`
     decides what to do (dispose VM, persist state).

4. **Create** `tests/DdsMonitor.Avalonia.Tests/Controls/MdiHostTests.cs`:
   - Add three children: assert `Children.Count == 3`.
   - `BringToFront` raises ZIndex above siblings.
   - Drag a child past the right edge: clamp keeps titlebar ≥ 40 px inside.
   - Resize a child below min: width clamps to 220.
   - Minimise a child: child's `IsVisible` is false, strip contains one item.
   - Restore from strip: child reappears, strip is empty.
   - `FocusNext(false)` with three children focuses the next-in-titlebar-order
     child. Cycling wraps.
   - `Remove("missing")` returns false and is a no-op.

**Implementation notes**:

- The minimised strip is in the host's template, not a sibling control.
- Children are added to `PART_ChildCanvas.Children`, not bound via an
  ItemsControl, because each child's positioning needs `Canvas.Left`/
  `Canvas.Top` set imperatively after drag/resize.
- `ChildGeometryChanged` fires after **every** drag or resize operation
  ends (on `PointerReleased`), so the workspace manager can debounce-save.

**Acceptance criteria**:

- [x] `dotnet build` succeeds with zero errors.
- [x] All `MdiHostTests` pass.
- [x] No reference to Dock.NET in `MdiHost.cs` or `MdiHost.axaml`.
- [x] No reference to `AvaloniaWindowManager` or any specific ViewModel —
  the host is reusable in isolation.

**Out of scope**:

- Workspace persistence integration (M1-T5).
- Animations on minimise/restore (deferred to polish).

---

## M1-T5 — New AvaloniaWindowManager

**Goal**: Rewrite `AvaloniaWindowManager` to orchestrate the three
`LayoutKind`s: MDI children in `MdiHost`, document tabs in the central
`DocumentDock`, and tool panes in side docks.

**Depends on**: M1-T1, M1-T3, M1-T4.

**Inputs to read**:

- `DESIGN.md` §3 (Hybrid Layout Model), §4 (MdiHost), §5 (Workspace
  Persistence), §6 (Threading).
- Existing `tools/DdsMonitor/DdsMonitor.Avalonia/AvaloniaWindowManager.cs`
  (replace it).
- Engine's `IWindowManager.cs` (the contract — do not change).
- Engine's `PanelState.cs` (the data record passed around).

**Deliverables**:

1. **Replace** `tools/DdsMonitor/DdsMonitor.Avalonia/AvaloniaWindowManager.cs`:
   - Class still implements `DdsMonitor.Engine.IWindowManager`.
   - Constructor injects `IAvaloniaViewRegistry`, `IServiceProvider`,
     `IEventBroker`, `IUiThreadInvoker`, plus the `IDockManager` (added in
     M1-T6) for document/tool-dock operations and an `MdiHost` reference
     (obtained lazily through a callback because the shell creates the
     host instance — see notes).

2. **API additions** beyond the engine interface (these are surfaced via a
   new `IAvaloniaWindowManager` interface declared in the shell project,
   not in Avalonia.Core — because they depend on `MdiHost` and Dock.NET
   types):
   - `PanelState SpawnPanel(string componentTypeName, LayoutKind layout, Dictionary<string, object>? initialState = null)`.
   - `void MoveToLayout(string panelId, LayoutKind newKind)`.
   - `void SetMdiHost(MdiHost host)` — called by the shell after the host
     is created.

3. **Behaviour**:
   - `SpawnPanel(name, state)` (interface version) defaults to
     `LayoutKind.Mdi`.
   - `SpawnPanel(name, Mdi, state)`:
     - Resolve VM type, create instance via DI (existing logic preserved).
     - Call `IStatefulViewModel.Initialize` if applicable.
     - Build view via `IAvaloniaViewRegistry`.
     - Create an `MdiChild`, set `Title` and `Content` and `ChildId`.
     - Read `ComponentState["__window"]` for geometry; default to
       `(80, 60, 600, 400)` if absent.
     - Call `MdiHost.Add(...)`.
   - `SpawnPanel(name, DockDocument, state)`:
     - Same view-building logic.
     - Hand off to `IDockManager.AddDocument(panelId, title, content)`.
   - `SpawnPanel(name, DockTool, state)`:
     - Same view-building logic.
     - Hand off to `IDockManager.AddTool(panelId, title, content, side)`
       where `side` defaults to `Left` (state may override via
       `ComponentState["__tool_side"]`).
   - `MoveToLayout(panelId, newKind)`:
     - Close current host placement, re-create in target placement,
       preserving the same VM instance and view.
   - `ClosePanel(panelId)`:
     - Remove from whichever host owns it; dispose the VM if `IDisposable`;
       fire `PanelClosed`; publish `WorkspaceSaveRequestedEvent`.
   - `SaveWorkspaceToJson()`:
     - Walk MDI children + dock documents + dock tools, build a `Panels`
       list with `LayoutKind` per panel.
     - Capture MDI geometry into `ComponentState["__window"]` including
       `IsMinimised`.
     - Add `DockLayout` field via `IDockManager.SerialiseLayout()`.
     - Preserve `ExcludedTopics` and `PluginSettings` exactly as before.
   - `LoadWorkspaceFromJson(json)`:
     - Restore `ExcludedTopics` first.
     - For each `Panel` entry, read `LayoutKind` (default `Mdi`),
       `SpawnPanel(name, kind, state)`.
     - Restore `DockLayout` via `IDockManager.DeserialiseLayout(json)` if
       present.
     - Publish `WorkspaceLoadedEvent` with `PluginSettings`.

4. **Create** `tests/DdsMonitor.Avalonia.Tests/AvaloniaWindowManagerTests.cs`:
   - Use a fake `IDockManager` and a real `MdiHost` instance in a headless
     window.
   - `SpawnPanel("TestVM", Mdi, null)` adds a child to the MDI host.
   - `SpawnPanel("TestVM", DockDocument, null)` calls the fake dock
     manager's `AddDocument`.
   - `MoveToLayout(id, DockDocument)` removes from MDI host and calls
     `AddDocument`, preserving the VM instance (assert same reference).
   - `ClosePanel(id)` disposes the VM (use a test VM that flips a flag).
   - `SaveWorkspaceToJson()` then `LoadWorkspaceFromJson(saved)` round-trips
     panel count, types, geometries, and `LayoutKind`.
   - A Blazor-format workspace (without `LayoutKind` or `DockLayout`)
     loads with every panel at `LayoutKind.Mdi`.

**Implementation notes**:

- The reason `SetMdiHost` exists: DI builds the WindowManager singleton at
  startup, before any Avalonia control is constructed. The shell's
  `OnFrameworkInitializationCompleted` creates the MDI host as part of
  `ShellWindow` and then calls `SetMdiHost`.
- The same pattern applies to the Dock manager — but `IDockManager` is
  designed in M1-T6 to be a DI-registered singleton that owns the
  `IDockFactory` and exposes operations; the shell wires the
  `MainWindow.DockControl` to it.
- All cross-thread call-ins are protected by `_uiThread.InvokeAsync(...)`.

**Acceptance criteria**:

- [x] `dotnet build` succeeds with zero errors.
- [x] All `AvaloniaWindowManagerTests` pass (≥ 6 named tests above).
- [x] The Blazor-workspace-compat test passes: a workspace JSON with no
  `LayoutKind` or `DockLayout` loads and produces only MDI children.
- [x] `WorkspaceSaveRequestedEvent` is published on panel close.

**Out of scope**:

- The Dock.NET implementation of `IDockManager` (M1-T6).
- Per-panel context menu integration (panels add their own context menus).

---

## M1-T6 — Dock.NET Integration

**Goal**: Add the Dock.NET layout root and an `IDockManager` service the
WindowManager calls into.

**Depends on**: M1-T0 (packages), M1-T4 (the MDI host that goes inside the
central document).

**Inputs to read**:

- `DESIGN.md` §3 (Hybrid Layout Model).
- Dock.NET 11.2 docs (latest). Especially: `RootDock`, `ProportionalDock`,
  `ToolDock`, `DocumentDock`, `Document`, `Tool`, `IFactory`.
- Reference example: any of the Dock.NET sample applications on GitHub.

**Deliverables**:

1. **Create** `tools/DdsMonitor/DdsMonitor.Avalonia/Docking/IDockManager.cs`:
   - Methods:
     - `void Initialise(DockControl dockControl, MdiHost mdiHost)` — called
       by the shell.
     - `void AddDocument(string id, string title, Control content)`.
     - `void AddTool(string id, string title, Control content, DockSide side)`.
     - `bool Remove(string id)`.
     - `bool TryFocus(string id)`.
     - `string SerialiseLayout()` and `void DeserialiseLayout(string json)`.
     - Event `event Action<string>? DocumentClosed`.
   - `DockSide` co-located enum: `{ Left, Right, Bottom }` (no Top — the
     menu/toolbar/transport row owns the top edge).

2. **Create** `tools/DdsMonitor/DdsMonitor.Avalonia/Docking/DockManager.cs`:
   - Implements `IDockManager`.
   - Internally owns a `DdsDockFactory` (subclass of `Dock.Model.Mvvm.Factory`).
   - On `Initialise`, builds the default layout: `RootDock` containing a
     `ProportionalDock` (orientation Horizontal) with a left `ToolDock`
     (proportion 0.2, visible only when at least one tool added), a centre
     `DocumentDock`, and a right `ToolDock` (visible only when ≥ 1 tool),
     with a bottom `ToolDock` underneath the proportional dock (only when
     ≥ 1 tool).
   - The centre `DocumentDock` contains one default `Document` with
     `Title="MDI Workspace"` and `Content` set to the supplied `MdiHost`.
     This document is **not closable** and **not draggable**.
   - `AddDocument` creates a new `Document` next to the MDI workspace
     document, sets its `Content` to the supplied `Control`, and gives it
     a sequential `Id`.
   - `AddTool` creates a new `Tool` in the matching side `ToolDock`,
     making it visible if previously collapsed.
   - Tool/document close in the UI fires `DocumentClosed(id)` so the
     WindowManager can dispose VMs.

3. **Create** `tools/DdsMonitor/DdsMonitor.Avalonia/Docking/DdsDockFactory.cs`:
   - Subclass of `Dock.Model.Mvvm.Factory`.
   - Override `CreateLayout()` to return the default root configured per
     above. Use a stable `Id` for the central document so layout
     deserialisation can find it again.

4. **Create** `tests/DdsMonitor.Avalonia.Tests/Docking/DockManagerTests.cs`:
   - Initialise a `DockManager` against a `DockControl` and an empty
     `MdiHost` in a headless window.
   - `AddDocument("a", "A", ...)` creates a document tab.
   - `AddTool("t", "T", ..., Left)` makes the left tool dock visible.
   - `Remove("a")` removes the document and fires `DocumentClosed`.
   - `SerialiseLayout()` then `DeserialiseLayout(saved)` round-trips
     visible documents and tools.

**Implementation notes**:

- The "MDI Workspace" document is special: its `CanClose` is false. It
  must always be present so the central area is never empty.
- Side tool docks are created up-front but with `IsCollapsed = true` (or
  whatever Dock.NET's equivalent is) so the UI shows nothing until a tool
  is added.
- Layout serialisation can use the built-in `Dock.Serializer` package if
  available; otherwise serialise manually using `System.Text.Json` on a
  layout DTO that captures `id`, `title`, `proportion`, and document/tool
  list per dock.

**Acceptance criteria**:

- [x] `dotnet build` succeeds with zero errors.
- [x] All `DockManagerTests` pass.
- [x] The central document is non-closable (verify via `CanClose` is false
  on the central document's `Document` instance after `Initialise`).
- [x] Empty side docks are not visible (`IsCollapsed` or equivalent is
  true) when no tools/documents are added.

**Out of scope**:

- Drag-to-dock from MdiHost into Dock.NET (later milestone — for now
  movement happens via the titlebar context menu only).

---

## M1-T7 — ShellWindow and App Composition

**Goal**: Replace the existing `ShellWindow` with a new shell that integrates
the menu bar with mnemonics, transport buttons, bandwidth/participant
indicators, the Dock.NET `DockControl`, and the `MdiHost` central document.

**Depends on**: M1-T2, M1-T4, M1-T5, M1-T6.

**Inputs to read**:

- `DESIGN.md` §3 (layout sketch at top), §7 (design tokens), §8 (keyboard).
- Existing `ShellWindow.axaml` and `ShellWindow.axaml.cs` (replace).
- Existing `App.axaml.cs` (modify to inject new services).
- Reference: `Components/Layout/MainLayout.razor` from the Blazor side for
  the menu structure, transport buttons, and bandwidth indicator.

**Deliverables**:

1. **Replace** `tools/DdsMonitor/DdsMonitor.Avalonia/ShellWindow.axaml`:
   - Outer `Window` with `Width=1280`, `Height=820`, `MinWidth=800`,
     `MinHeight=500`, `Title="DDS Monitor"`.
   - `DockPanel` as root.
   - **Top dock**: a `Grid` with three columns: menu (auto), transport
     (auto), status (star). Inside:
     - Column 0: `Menu x:Name="MainMenu"` with four top-level items
       (`_File`, `_View`, `_Devel`, `_Plugins`). Items are built per
       DESIGN.md §3 menu structure with mnemonics:
       - File: `_Topic Sources…`, `_Plugin Manager…`, separator,
         `_Reset Layout`, `_Export Layout…`, `_Import Layout…`,
         separator, `E_xit`.
       - View: dynamic — populated from `IMenuRegistry` plus a fixed
         `_Theme` submenu (`_System`, `_Light`, `_Dark`).
       - Devel: `_Enable Self-Sending` toggle, `Self-Send _Rate` submenu
         (`_1 Hz`, `_10 Hz`, `_100 Hz`, `1 _kHz`, `10 k_Hz`), separator,
         `_Perf Stats…`.
       - Plugins: populated from `IMenuRegistry`'s "Plugins"-rooted entries
         or the plugin panel registry (Blazor parity).
     - Column 1: three `Button`s for transport (`▶`, `⏸`, `⏹`) with
       `ToolTip.Tip="Play (Space)"`, `"Pause (Space)"`, `"Reset (F5)"`.
     - Column 2: a horizontal `StackPanel` right-aligned with:
       - Running/Paused indicator: `Ellipse Width=10 Height=10 Fill={DynamicResource Accent.Receiving}` with green/orange swap via a binding to `DdsBridge.IsPaused`.
       - `TextBlock` "Running"/"Paused".
       - `TextBlock` bandwidth (e.g. "1.2 MB/s") updated at 1 Hz.
       - `Button` participant summary "DomainId=0" opens `NetworkConfigView`.
   - **Bottom dock**: a status bar `Border` height 24 with a `TextBlock`
     `Ready`.
   - **Centre fill**: the Dock.NET `DockControl x:Name="MainDock"`.

2. **Replace** `tools/DdsMonitor/DdsMonitor.Avalonia/ShellWindow.axaml.cs`:
   - Constructor takes `IServiceProvider services` and resolves what it
     needs.
   - In `Initialize` (or constructor after `InitializeComponent`):
     - Create an `MdiHost` instance.
     - Resolve `IDockManager`, call `Initialise(MainDock, mdiHost)`.
     - Resolve `IAvaloniaWindowManager`, call `SetMdiHost(mdiHost)`.
     - Build the menu items dynamically from `IMenuRegistry` (replicating
       the existing `RebuildMenu` logic, but with mnemonics).
     - Wire transport `Click` handlers to `IDdsBridge`.
     - Wire `IKeyboardShortcutService` to install `KeyBinding`s on the
       window (Space, F5, Ctrl+W, Ctrl+1..9, Ctrl+Tab, Alt+F4 via the
       native window).
     - Start a `DispatcherTimer` at 1 Hz to update bandwidth, samples/sec,
       and the running indicator.
   - Bandwidth helper:
     ```
     private static string FormatBandwidth(long bps) {
         if (bps <= 0) return "0 B/s";
         if (bps < 1024) return $"{bps} B/s";
         if (bps < 1_048_576) return $"{bps / 1024.0:0.##} KB/s";
         return $"{bps / 1_048_576.0:0.##} MB/s";
     }
     ```
     Port from `MainLayout.razor`.

3. **Modify** `tools/DdsMonitor/DdsMonitor.Avalonia/App.axaml.cs`:
   - On `OnFrameworkInitializationCompleted`:
     - Resolve `IServiceProvider`.
     - Create `ShellWindow(services)` and set as `MainWindow`.
     - After the window is shown, resolve `IWindowManager` and call
       `LoadWorkspace(workspaceState.WorkspaceFilePath)` (existing logic).

4. **Create** `tests/DdsMonitor.Avalonia.Tests/ShellWindowTests.cs`:
   - Headless test that builds a service provider with the real services,
     constructs a `ShellWindow`, and asserts:
     - Menu bar has exactly four top-level items.
     - Pressing `Space` toggles `IDdsBridge.IsPaused`.
     - Pressing `F5` calls `IDdsBridge.ResetAll`.
     - The bandwidth TextBlock updates after a sample is published (use a
       fake bridge that returns a non-zero `TotalBytesReceived`).
     - Transport buttons' `ToolTip.Tip` contains the keyboard shortcut
       label.

**Implementation notes**:

- For the menu mnemonics: in Avalonia, the `Header` text uses `_` to mark
  the mnemonic — same as WPF. Make sure `MainMenu.IsTabStop="False"`.
- The dynamic plugin menu items: subscribe to `IMenuRegistry.Changed`
  (debounce via a 100 ms timer) and rebuild on change.
- For the participant summary, format as `"Domain=" + string.Join(",", participantIds)`
  with a max length of 30 chars then ellipsis.
- The "Reset Layout" file-menu item resets the dock layout to the default
  (calls `DockManager.Initialise` again with no saved state) AND clears
  the MDI host. Confirmed via a Yes/No `MessageBox` (use a small custom
  dialog; OS native message boxes are not in Avalonia 11 core — use
  `Avalonia.Controls.MessageBoxManager` or build a 30-line dialog).

**Acceptance criteria**:

- [x] `dotnet build` succeeds with zero errors.
- [x] All `ShellWindowTests` pass.
- [x] At runtime, launching the app with **no saved workspace** shows the
  empty shell described in §3 (menu, transport, status bar, empty dock
  layout, empty MDI host).
- [x] The known bug from DESIGN.md §13 item 1 (toolbar `Content=Tooltip`)
  is fixed: toolbar buttons now show their `Label` (or icon glyph) with
  `Tooltip` in `ToolTip.Tip`.
- [x] The known bug from DESIGN.md §13 item 5 (dead `ContentArea`) is
  fixed: there is no orphan `ContentArea` Grid in the new shell.
- [x] All four top-level menu items work via mouse and via Alt+letter.

**Out of scope**:

- Implementing the panels that the menu items open (other than what
  M1-T10 brings — see Feature Demo). Menu items that open panels for
  panels that don't exist yet show a "Coming in M*n*" placeholder dialog.

---

## M1-T8 — Service Implementations

**Goal**: Implement the new service interfaces declared in M1-T1.

**Depends on**: M1-T1, M1-T7 (theme service interacts with the live window).

**Inputs to read**:

- `DESIGN.md` §9 (Services).
- The interface declarations from M1-T1.

**Deliverables**:

All in `tools/DdsMonitor/DdsMonitor.Avalonia/Services/`.

1. **Create** `ContextMenuPresenter.cs`:
   - Implements `IContextMenuPresenter`.
   - On `Show(anchor, dataContext, defaultItems)`:
     - Get items from `IContextMenuRegistry.GetItems(dataContext)`.
     - Combine: `defaultItems.Concat(registry items).Distinct(by id)`.
     - Build a fresh Avalonia `ContextMenu`, populate with `MenuItem`s.
     - Each `MenuItem` `Click` invokes the corresponding
       `ContextMenuItem.OnExecute` on the UI thread.
     - Attach to `anchor.ContextMenu` (or create a transient flyout and
       open it manually).
     - Open at the current pointer position.

2. **Create** `FileDialogService.cs`:
   - Implements `IFileDialogService`.
   - Uses `TopLevel.GetTopLevel(anchor).StorageProvider`. Because we don't
     have an anchor in the service constructor, the constructor takes
     `Func<Visual> rootProvider` — the shell registers `() => MainWindow`.
   - Maps `FilePickerFilter` to `FilePickerFileType` per Avalonia 11 API.

3. **Create** `KeyboardShortcutService.cs`:
   - Implements `IKeyboardShortcutService`.
   - Maintains a `Dictionary<KeyGesture, RegisteredShortcut>`.
   - `TryInvoke(gesture)` looks up and invokes on the UI thread.
   - `Registered` returns a snapshot list (for help dialogs).

4. **Create** `ThemeService.cs`:
   - Implements `IThemeService`.
   - On construction reads mode from `IUserSettings.Get("Theme", "Mode", ThemeMode.System)`.
   - `SetMode` updates `Application.Current.RequestedThemeVariant` to the
     matching `ThemeVariant`, persists via `IUserSettings.Set` then
     `SaveAsync`, fires `ModeChanged`.
   - `ThemeMode.System` maps to `ThemeVariant.Default`.

5. **Create** `ClipboardService.cs`:
   - Implements `IClipboardService`.
   - Constructor takes `Func<TopLevel?> topLevelProvider` (shell wires it).
   - `SetTextAsync(text)` → `TopLevel.Clipboard?.SetTextAsync(text)`.
   - `GetTextAsync()` → returns the clipboard text or null.

6. **Create** tests in `tests/DdsMonitor.Avalonia.Tests/Services/`:
   - `KeyboardShortcutServiceTests.cs`: register a gesture, invoke it,
     assert the action ran.
   - `ThemeServiceTests.cs`: setting `Dark` updates
     `Application.Current.RequestedThemeVariant` to `ThemeVariant.Dark`,
     persists via `IUserSettings`, raises `ModeChanged`.
   - `ContextMenuPresenterTests.cs`: register two items via a fake
     `IContextMenuRegistry`, pass one default item, assert the resulting
     `ContextMenu` has three items in the right order.

**Implementation notes**:

- `FileDialogService` and `ClipboardService` need a `Visual` / `TopLevel`
  reference. Pass them lazily because the shell's main window may not
  exist when the services are constructed in DI.

**Acceptance criteria**:

- [x] `dotnet build` succeeds with zero errors.
- [x] All Services tests pass.
- [x] Each service interface has exactly one implementation under
  `Services/`.
- [x] Setting the theme to Dark at runtime changes the rendered theme
  (manually verified is OK; the unit test only checks the side-effects).

**Out of scope**:

- Hook-up to the actual menu items (M1-T7 wires Theme menu).

---

## M1-T9 — Program.cs, DI, and Bug Fixes

**Goal**: Update `Program.cs` to register all the new services, fix the
double-`BuildAvaloniaApp` bug, fix the remaining DESIGN.md §13 bugs that
aren't covered by other M1 tasks.

**Depends on**: M1-T1, M1-T5, M1-T6, M1-T7, M1-T8.

**Inputs to read**:

- `DESIGN.md` §9 (Services), §13 (Known Bugs).
- Existing `Program.cs`.
- Existing `AvaloniaTypeDrawerRegistry.cs` (bug 3).
- Existing `TopicExplorerViewModel.cs` (bug 4).

**Deliverables**:

1. **Modify** `tools/DdsMonitor/DdsMonitor.Avalonia/Program.cs`:
   - Single call to `BuildAvaloniaApp(host.Services)` reused for both
     `LogToTrace` and `StartWithClassicDesktopLifetime`.
   - DI registrations (singleton unless noted):
     - All existing engine services via `AddDdsMonitorServices`.
     - All Avalonia.Core singletons that are already there.
     - **New** singletons:
       - `IUiThreadInvoker → AvaloniaUiThreadInvoker`
       - `IContextMenuPresenter → ContextMenuPresenter`
       - `IFileDialogService → FileDialogService` (lazy root provider)
       - `IKeyboardShortcutService → KeyboardShortcutService`
       - `IThemeService → ThemeService`
       - `IClipboardService → ClipboardService` (lazy top-level provider)
       - `IDockManager → DockManager`
       - `IAvaloniaWindowManager → AvaloniaWindowManager` and
         `IWindowManager → resolves the same instance via factory`.
   - Wire the lazy providers: `FileDialogService` and `ClipboardService`
     get factories that return the live main window once it exists.

2. **Modify** `tools/DdsMonitor/DdsMonitor.Avalonia.Core/AvaloniaTypeDrawerRegistry.cs`
   to fix bug §13 item 3:
   - In `BuildFallback`'s `box.LostFocus` handler, change
     `ctx.OnChange(box.Text)` to:
     ```
     try {
         var converted = Convert.ChangeType(box.Text, capturedProp.PropertyType);
         ctx.OnChange(converted);
     } catch {
         // Leave current value unchanged.
     }
     ```

3. **Modify** `tools/DdsMonitor/DdsMonitor.Avalonia.StandardPlugin/TopicExplorerViewModel.cs`
   to fix bug §13 item 4:
   - Remove the constructor's call to `RefreshTopics()`.
   - Keep only the `Initialize` call to `RefreshTopics()`.
   - Verify no other code path requires `Topics` to be populated before
     `Initialize` runs.

4. **Modify** `tools/DdsMonitor/DdsMonitor.Avalonia.StandardPlugin/TopicExplorerPlugin.cs`:
   - Remove the auto-spawn at end of `Initialize` (the line
     `windowManager.SpawnPanel(nameof(TopicExplorerViewModel), null);`).
   - Per DESIGN.md §3, first launch is empty.

5. **Create** `tests/DdsMonitor.Avalonia.Tests/ProgramRegistrationTests.cs`:
   - Build a service provider with `AddDdsMonitorServices` + the new
     Avalonia registrations.
   - Resolve each of: `IUiThreadInvoker`, `IContextMenuPresenter`,
     `IFileDialogService`, `IKeyboardShortcutService`, `IThemeService`,
     `IClipboardService`, `IDockManager`, `IWindowManager`,
     `IAvaloniaWindowManager`.
   - Assert all resolutions succeed (no `null`, no
     `InvalidOperationException`).
   - Assert that `IWindowManager` and `IAvaloniaWindowManager` resolve to
     the same instance.

**Acceptance criteria**:

- [x] `dotnet build` succeeds with zero errors.
- [x] All `ProgramRegistrationTests` pass.
- [x] Searching the codebase for `BuildAvaloniaApp(host.Services)` returns
  exactly one occurrence inside `Program.cs`.
- [x] The reflection fallback in `AvaloniaTypeDrawerRegistry` now correctly
  propagates the converted value (add a unit test in
  `DdsMonitor.Avalonia.Core.Tests` that registers no drawer for a custom
  type, builds the fallback, sets text "42" on the box, raises LostFocus,
  and asserts the `onChange` callback received the integer `42`).
- [x] `TopicExplorerViewModel` constructor does not call `RefreshTopics`.
- [x] `TopicExplorerPlugin.Initialize` does not auto-spawn.

**Out of scope**:

- New panels — covered by other milestones / M1-T10.

---

## M1-T10 — FeatureDemo Plugin

**Goal**: Build a rich diagnostic plugin that exercises every panel and
every DDS field-type drawer the standard plugin offers. This is the smoke
test for the entire port.

**Depends on**: M1-T0 (skeleton), M1-T1 (registries), M1-T9 (DI wiring).

**Inputs to read**:

- `DESIGN.md` §10 (Plugin Loading).
- Existing `DdsMonitor.Plugins.FeatureDemo` (Blazor — types may be
  partly reusable, but UI parts are Razor and must be reimplemented).
- Existing `DdsMonitor.Avalonia.StandardPlugin/HeartbeatSample.cs` — gives
  an example of `[DdsTopic]`/`[DdsKey]` attribute usage.
- `CycloneDDS.Schema` attributes (`DdsTopic`, `DdsKey`, `DdsUnion`,
  `DdsCase`, `DdsDiscriminator`, `DdsDefaultCase`, `DdsQos`) — search for
  attribute definitions in the engine/runtime to see what's available.

**Deliverables**:

All under `tools/DdsMonitor/DdsMonitor.Avalonia.FeatureDemoPlugin/`.

1. **Delete** the `Placeholder.cs` created in M1-T0.

2. **Create** DDS topic types — file `DemoTypes.cs`:
   - `TelemetrySample` (unkeyed, fast): fields `long Timestamp`, `int SequenceId`, `double Cpu`, `double Memory`, `float Temperature`.
   - `EntityState` (keyed): `[DdsKey] int EntityId`, `string Name`, `EntityKind Kind` (enum), `float X`, `float Y`, `float Z`, `byte Health`, `bool IsAlive`.
   - `EntityKind` enum: `Player`, `Npc`, `Vehicle`, `Projectile`.
   - `AlertEvent` (unkeyed, slow, rare): `long Timestamp`, `Severity Level`, `string Message`, `string Origin`.
   - `Severity` enum: `Info`, `Warning`, `Error`, `Critical`.
   - `GeoLocation` (unkeyed, nested struct): `double Latitude`, `double Longitude`, `float Altitude`, `Address NestedAddress`.
   - `Address` struct (not a topic, just nested): `string Street`, `string City`, `string Country`.
   - `UnionPayload` (unkeyed, demonstrates DDS union):
     ```
     [DdsTopic, DdsUnion]
     public struct UnionPayload {
         [DdsDiscriminator] public int Discriminator;
         [DdsCase(1)] public int IntValue;
         [DdsCase(2)] public string StringValue;
         [DdsCase(3)] public double DoubleValue;
         [DdsDefaultCase] public bool DefaultValue;
     }
     ```
     If `DdsUnion` etc. don't exist exactly as named, look them up in
     `DetailPanel.razor`'s `@inject`s and `using` clauses — they are
     referenced there.

3. **Create** `DemoPublisherService.cs`:
   - Implements `IHostedService` and `IDisposable`.
   - On `StartAsync`: registers all five topic types via `ITopicRegistry`,
     acquires writers via `IDdsBridge`. Runs a background task per topic:
     - Telemetry @ 10 Hz: random CPU/memory/temperature with realistic
       drift.
     - EntityState @ 5 Hz for 8 distinct keys: positions move in
       small steps, health decays slowly, IsAlive flips when Health=0.
     - Alerts every 7 seconds, Severity cycling.
     - GeoLocation every 2 seconds: a random walk over Europe coordinates
       with a populated nested `Address`.
     - UnionPayload every 3 seconds: cycles through all four discriminator
       branches.
   - Configurable on/off via `FeatureDemoPlugin:Enabled` config setting
     (default `true`).
   - Toggleable at runtime via `ToggleEnabled()`.

4. **Create** `FeatureDemoDashboardViewModel.cs` and `FeatureDemoDashboardView.axaml`:
   - A panel that displays:
     - Total samples published by topic (poll via `ISampleStore.GetTopicCount`).
     - A textual "Publisher state" toggle button.
     - A small live log of the last 10 published alerts (subscribe to
       `IDdsBridge` reader for `AlertEvent`).
   - Layout: `Grid RowDefinitions="Auto,Auto,*"`. Top: toggle button.
     Middle: 5-row table of topic name + count (binding refreshes at 1 Hz
     via a `DispatcherTimer`). Bottom: a `ListBox` of recent alerts with
     severity-coloured chips.

5. **Create** `FeatureDemoPlugin.cs`:
   - Implements `IMonitorPlugin`.
   - `Name = "FeatureDemo"`, `Version = "1.0"`.
   - `ConfigureServices` registers `DemoPublisherService` as both
     singleton and hosted.
   - `Initialize`:
     - Get `IMenuRegistry`, `IWindowManager`, `IAvaloniaViewRegistry`,
       `DemoPublisherService`.
     - Register `FeatureDemoDashboardViewModel → FeatureDemoDashboardView`.
     - Add menu items:
       - `Devel → _Feature Demo → _Toggle Publisher` (calls
         `DemoPublisherService.ToggleEnabled`).
       - `View → Feature _Demo Dashboard` (spawns
         `FeatureDemoDashboardViewModel` panel as MDI child).

6. **Create** tests under
   `tests/DdsMonitor.Avalonia.FeatureDemoPlugin.Tests/`:
   - `DemoPublisherServiceTests.cs`: a unit test with a fake
     `IDdsBridge` and `ITopicRegistry`. Start the service, advance time
     500 ms via test scheduler, stop. Assert at least one `Write` call
     was made on each of the five topic writers.
   - `FeatureDemoDashboardViewModelTests.cs`: with a fake
     `ISampleStore` returning known counts, the topic-count rows match.

**Implementation notes**:

- Random number gen: seed with `42` so test runs are deterministic.
- Don't try to subscribe to your own published topics — use
  `ISampleStore.GetTopicCount` to read counts.
- The publisher service must shut down promptly on
  `IHostApplicationLifetime.ApplicationStopping`.

**Acceptance criteria**:

- [x] `dotnet build` succeeds with zero errors.
- [x] After a build, `plugins/DdsMonitor.Avalonia.FeatureDemoPlugin.dll`
  is present in the shell's bin folder.
- [x] All FeatureDemo tests pass.
- [x] Running the app: `Devel → Feature Demo → Toggle Publisher` toggles
  publishing; running with default config, all five topics show up in the
  (M2-pending) topic explorer — verify via a temporary debug log that
  `ITopicRegistry.AllTopics` contains the five registered types.
- [x] `View → Feature Demo Dashboard` opens an MDI child showing live
  counts increasing at the expected rates.

**Out of scope**:

- Refactoring or porting the legacy Blazor `DdsMonitor.Plugins.FeatureDemo`
  project (it stays untouched; the new project lives alongside it).

---

## M1-T11 — StandardPlugin Touch-ups

**Goal**: Update the existing StandardPlugin code to match the new
toolbar entry signature and remove the auto-spawn logic (the latter is
already covered in M1-T9 — this task is the compile-fix sweep).

**Depends on**: M1-T1 (toolbar entry change), M1-T9.

**Inputs to read**:

- DESIGN.md §13 (Known Bugs) items 1, 6, 7.
- All existing `*Plugin.cs` in
  `tools/DdsMonitor/DdsMonitor.Avalonia.StandardPlugin/` for `Register(...)`
  call sites.

**Deliverables**:

1. **Sweep** all `IToolbarRegistry.Register(...)` call sites in the
   StandardPlugin and update to the new signature (positional `label`
   added).
2. **Audit** every `IMenuRegistry.AddMenuItem(...)` call to ensure menu
   labels include a mnemonic (a leading `_` for the appropriate letter).
   E.g. `"Schema Sources\u2026"` should become `"Schema _Sources…"`.
3. **Verify** no `*Plugin.cs` calls `windowManager.SpawnPanel(...)` from
   `Initialize` itself (auto-spawn). This was done in M1-T9 for
   `TopicExplorerPlugin`; double-check the others.

**Acceptance criteria**:

- [x] `dotnet build` succeeds with zero errors.
- [x] Every existing toolbar registration in StandardPlugin uses the new
  positional signature.
- [x] Every StandardPlugin menu label that was previously without a
  mnemonic now has one (no double mnemonics within a single menu —
  verify visually).
- [x] No `SpawnPanel` call inside an `Initialize` method in any plugin
  (`grep -r "SpawnPanel" tools/DdsMonitor/DdsMonitor.Avalonia.StandardPlugin/`
  shows only context-menu and menu-handler-action calls, not initialise-time).

**Out of scope**:

- Improving the StandardPlugin panel features themselves (M2+).

---

## M1-T12 — M1 End-to-End Smoke Test

**Goal**: Prove the M1 deliverables compose correctly with an end-to-end
test, and produce a brief manual-test checklist for the user.

**Depends on**: all of M1-T0 through M1-T11.

**Inputs to read**:

- All previous M1 tasks' acceptance criteria.

**Deliverables**:

1. **Create** `tests/DdsMonitor.Avalonia.Tests/Smoke/M1SmokeTest.cs`:
   - One `[Fact]` test, `M1_ColdStart_ProducesEmptyShell`:
     - Spin up the full DI graph as the app does.
     - Construct `ShellWindow`, render headless for 1 second.
     - Assert: window title is "DDS Monitor", menu has 4 items, transport
       buttons are present, dock root has zero documents besides the MDI
       workspace, MDI host has zero children.
   - One `[Fact]` test, `M1_FeatureDemo_PublishesAllFiveTopics`:
     - Spin up DI + run hosted services for 1.5 s.
     - Assert: `ITopicRegistry.AllTopics` contains 5 types from the demo
       plugin; `ISampleStore.GetTopicCount` for each is > 0.
   - One `[Fact]` test, `M1_WorkspaceRoundtrip_BlazorJsonStillLoads`:
     - Construct a small JSON in the Blazor schema (one `SamplesViewer`
       panel with no `LayoutKind`, no `DockLayout`).
     - Load via `IWindowManager.LoadWorkspaceFromJson`.
     - Assert: the panel spawns as `LayoutKind.Mdi`; geometry from
       `__window` applied; saved JSON now contains `LayoutKind=Mdi` for
       that panel and a serialised `DockLayout`.

2. **Create** `docs/M1-MANUAL-TEST-CHECKLIST.md`:
   - 15–20-item checklist the human user runs after the agent reports
     M1 complete. Items the headless tests can't easily cover:
     - App launches without errors on Windows / Linux / macOS.
     - Menu mnemonics work (Alt+F opens File menu).
     - Theme toggle works (View → Theme → Dark).
     - Resize the main window — MDI children clamp to stay visible.
     - Open FeatureDemo Dashboard — counts increase live.
     - Open multiple MDI children, drag them around, raise to front by
       clicking, minimise/restore from strip.
     - Right-click an MDI titlebar — context menu has six items.
     - "Dock as tab" moves the child to a Dock.NET tab; the VM state is
       preserved.
     - Close app — workspace JSON file is written.
     - Reopen app — workspace restores including dock state and MDI
       positions.
     - Keyboard: Space toggles Pause; F5 resets; Ctrl+W closes focused
       MDI child; Ctrl+1..9 focuses *n*th child; Ctrl+Tab cycles.
     - Plugins list in the Plugins menu reflects FeatureDemo plus any
       legacy demo plugins discovered.

**Acceptance criteria**:

- [x] `dotnet test` for the smoke test file: 3/3 passing.
- [x] The manual test checklist is present and lists at least 15 items.
- [x] A final-build of the solution produces an exe under
  `tools/DdsMonitor/DdsMonitor.Avalonia/bin/Debug/net8.0/` that launches
  to the empty shell described in DESIGN.md §3 with the FeatureDemo
  publishing.

**Out of scope**:

- Fixing any issue surfaced by the manual checklist — that becomes a
  follow-up task in the M1 wrap-up. If a checklist item fails, **stop
  and report**, do not silently patch.

---

## M1 Finish-Up (M1-T13 — M1-T20)

The first pass of M1 (M1-T0 through M1-T12) produced a structurally correct
skeleton but left several functional gaps that block the M1 acceptance
criteria and would compound through M2. These tasks close those gaps.

Each finish-up task has the same structure as the original M1 tasks. They
are sequenced so that an agent can implement them in numeric order without
needing to revisit earlier ones, but the dependencies are explicit.

> **Audit context**: the issues these tasks address were found by reviewing
> the post-M1 source dump against `DESIGN.md`. References to "the original
> M1 task that introduced the file" are given so the agent can locate the
> existing code without searching.

---

## M1-T13 — Install Keyboard Shortcuts on the Shell Window

**Goal**: Make the keyboard shortcuts registered with
`IKeyboardShortcutService` actually fire when the user presses the key.
Currently, `KeyboardShortcutService.Register(Space, …)` is called but no
`KeyBinding` is ever installed on the `ShellWindow`, so the service is a
data structure with no input pipe.

**Depends on**: M1-T7 (ShellWindow), M1-T8 (KeyboardShortcutService).

**Inputs to read**:

- `DESIGN.md` §8 (Keyboard Navigation) — the authoritative shortcut list.
- Current `ShellWindow.axaml.cs` registration block (the agent currently
  registers Space and F5 only).
- Current `KeyboardShortcutService.cs` — note `TryInvoke` already
  dispatches via `Dispatcher.UIThread.Post`.

**Deliverables**:

1. **Extend** `IKeyboardShortcutService` (`DdsMonitor.Avalonia.Core/IKeyboardShortcutService.cs`):
   - Add event `event Action<RegisteredShortcut>? Registered2`
     (renamed if naming clashes; the existing `Registered` property must
     keep its name). Suggested name: `event Action<RegisteredShortcut>? ShortcutAdded`.
   - Fire `ShortcutAdded` from `Register(...)` after the entry is appended.
   - Rationale: the shell installs key bindings dynamically as plugins
     register new shortcuts; without an event the shell would have to poll.

2. **Modify** `ShellWindow.axaml.cs`:
   - In the constructor, after resolving `IKeyboardShortcutService`,
     subscribe to `ShortcutAdded` and call a new private method
     `InstallShortcut(RegisteredShortcut)`.
   - `InstallShortcut` builds an `Avalonia.Input.KeyBinding` with the
     gesture and a `RoutedCommand` whose handler invokes the shortcut's
     `Action` on the UI thread, then appends to
     `this.KeyBindings`.
   - For shortcuts registered *before* the subscription, iterate
     `shortcutService.Registered` once and install each.
   - Register the full shortcut set from DESIGN.md §8 *Global shortcuts*
     in the shell constructor (after the service is wired):

     | Gesture | Description | Action |
     |---|---|---|
     | `Space` | Play/Pause | toggle `IDdsBridge.IsPaused` |
     | `F5` | Reset | `IDdsBridge.ResetAll()` |
     | `Ctrl+W` | Close focused MDI child | call `IAvaloniaWindowManager.ClosePanel(id)` for the focused MDI child id (see notes) |
     | `Ctrl+Shift+T` | Reopen last closed panel | re-spawn from a `Stack<PanelState>` maintained in the shell (LIFO) |
     | `Ctrl+1` through `Ctrl+9` | Focus *n*th MDI child | call `MdiHost.Children[n-1].Focus()` if it exists |
     | `Ctrl+Tab` | Cycle MDI children forward | `MdiHost.FocusNext(reverse: false)` |
     | `Ctrl+Shift+Tab` | Cycle MDI children backward | `MdiHost.FocusNext(reverse: true)` |
     | `Ctrl+,` | Open Plugin Manager | placeholder dialog "Coming in M5" |
     | `Escape` | Cancel active interaction | bubbles through normal focus chain — no global handler needed |

     `Ctrl+T` ("Open new SamplesViewer for the topic selected in
     TopicExplorer") is **deferred to M2** because it requires
     TopicExplorer to track selection state.

   - For *closed-panel restoration* (`Ctrl+Shift+T`):
     - Subscribe to `IWindowManager.PanelClosed`.
     - On each event, push the closed `PanelState` (and its captured
       `LayoutKind` taken from a parallel dict in `AvaloniaWindowManager`'s
       `_layoutKinds` cache before removal) onto a `Stack<(PanelState, LayoutKind)>`.
     - `Ctrl+Shift+T` pops and calls `SpawnPanel(name, layout, state.ComponentState)`.
     - The stack is capped at 16 entries (FIFO eviction at the bottom).
     - The `LayoutKind` capture requires a small interface addition: add
       `LayoutKind GetLayoutKind(string panelId)` to `IAvaloniaWindowManager`
       returning `LayoutKind.Mdi` when not found. Implement it by reading
       `_layoutKinds` under the existing lock.

3. **Locate the currently focused MDI child** for `Ctrl+W`:
   - Add `string? FocusedChildId` to `MdiHost` — a property updated when
     a child receives keyboard focus (subscribe to each added child's
     `GotFocus` and propagate `IsKeyboardFocusWithin` changes).
   - `Ctrl+W` handler reads `FocusedChildId`. If null, no-op.

4. **Tests** under `tests/DdsMonitor.Avalonia.Tests/`:
   - `ShellWindowShortcutTests.cs`:
     - Headless construction of `ShellWindow` + DI graph.
     - Simulate `KeyDown` for Space: assert `IDdsBridge.IsPaused` flipped.
     - Simulate `KeyDown` for F5: assert `IDdsBridge.ResetAll` invoked
       (use a fake bridge that records calls).
     - Register a new shortcut at runtime via the service: simulate the
       new gesture and assert it fires (covers `ShortcutAdded` wiring).
     - Open three MDI children, simulate `Ctrl+W` while one is focused:
       assert that child closed.
     - Close all three via `Ctrl+W`, then `Ctrl+Shift+T` three times:
       assert each re-spawns in reverse-close order.

**Implementation notes**:

- `KeyBinding` requires an `ICommand`. Implement a simple
  `ActionCommand : ICommand` co-located in
  `DdsMonitor.Avalonia/Services/` if one isn't already present.
- Avalonia's modifier matching is case-sensitive in some versions; use
  `KeyGesture.Parse("Ctrl+W")` to construct gestures consistently.
- `Ctrl+Tab` is normally intercepted by Avalonia's `TabControl` focus
  navigation. Attach the binding at the `Window` level with
  `HotKeyManager.SetHotKey` if necessary, or handle in
  `OnKeyDown` with `e.Handled = true`.

**Acceptance criteria**:

- [x] `dotnet build` succeeds with zero errors.
- [x] All `ShellWindowShortcutTests` pass.
- [x] Manually pressing Space while the shell is focused (no textbox
  focused) toggles the bridge's paused state — confirmable via the
  status text updating to "Paused" / "Running" within 1 second.
- [x] `Ctrl+Shift+T` after closing an MDI child re-spawns it at the same
  geometry and same `LayoutKind`.
- [x] Pressing `Ctrl+W` while focus is inside an MDI child closes that
  child; pressing `Ctrl+W` with no MDI child focused is a no-op.

**Out of scope**:

- `Ctrl+T` (selection-aware, deferred to M2).
- Quick-open palette `Ctrl+Shift+P` (M5).

---

## M1-T14 — Implement `MoveToLayout` and Wire the Titlebar Context Menu

**Goal**: Make the right-click context menu on the `MdiChild` titlebar
actually move panels between layout kinds. Currently
`AvaloniaWindowManager.MoveToLayout` throws `NotSupportedException` with
"deferred to M2", and the `MdiChild.LayoutKindRequested` event has no
subscriber — the menu items do nothing.

**Depends on**: M1-T5 (WindowManager), M1-T3 (MdiChild), M1-T6 (DockManager).

**Inputs to read**:

- `DESIGN.md` §3 (Hybrid Layout Model) — the three `LayoutKind`s.
- Existing `AvaloniaWindowManager.MoveToLayout` — replace the `throw`.
- Existing `MdiChild.LayoutKindRequestedEvent` and the `MdiChildLayoutKindEventArgs`
  with `TargetKind` and `SideHint`.

**Deliverables**:

1. **Implement** `AvaloniaWindowManager.MoveToLayout(string panelId, LayoutKind newKind)`:
   - Look up `_layoutKinds[panelId]`. If equal to `newKind`, no-op.
   - Find the existing view+VM pair from `_viewModels[panelId]` and
     rebuild the view content via `_viewRegistry.BuildView(vm)` (the
     existing view control is destroyed in the old host and a fresh
     control is built for the new host — necessary because Avalonia
     controls cannot have two parents).
   - **From MDI to Dock**:
     - Capture current geometry from `Canvas.GetLeft/Top` and `child.Width/Height`
       and write into `panelState.ComponentState["__window"]` so a later
       "back to MDI" restores it.
     - Call `_mdiHost.Remove(panelId)` but **without** triggering
       `OnPanelRemoved` (which would dispose the VM and fire `PanelClosed`).
       To do this safely, add an internal overload
       `MdiHost.RemoveWithoutDisposing(string id)` that returns the
       MdiChild detached but doesn't raise `ChildRemoved`. The
       `WindowManager` then handles VM lifetime itself.
     - Call `_dockManager.AddDocument(panelId, panelState.Title, freshView)`
       or `_dockManager.AddTool(panelId, panelState.Title, freshView, side)`
       where `side` is decoded from the `MdiChildLayoutKindEventArgs.SideHint`
       passed through to `MoveToLayout` — see point 3.
   - **From Dock to MDI**:
     - Call `_dockManager.Remove(panelId)`, but DocumentClosed event
       already fires — for the cross-layout case we suppress it via a
       short-lived `_movingPanels: HashSet<string>` guard that
       `OnPanelRemoved` checks at the top and short-circuits.
     - Build a new `MdiChild` and call `_mdiHost.Add(...)` using the
       geometry from `panelState.ComponentState["__window"]` (or a default).
   - **Between two dock kinds** (DockDocument ↔ DockTool):
     - `_dockManager.Remove(panelId)` with the guard, then add to the
       other kind.
   - Update `_layoutKinds[panelId] = newKind`.
   - Publish `WorkspaceSaveRequestedEvent`.

2. **Add `MoveToLayout(panelId, newKind, sideHint)` overload** on
   `IAvaloniaWindowManager` (optional `string? sideHint = null`):
   - Only meaningful when `newKind == DockTool`; ignored otherwise.
   - Maps `"left" → DockSide.Left`, `"right" → DockSide.Right`,
     `"bottom" → DockSide.Bottom`, anything else → `DockSide.Left`.

3. **Subscribe to `MdiChild.LayoutKindRequested` from the WindowManager**:
   - When `SpawnMdiPanel` creates an `MdiChild`, attach a handler:
     ```
     child.LayoutKindRequested += (sender, e) =>
         MoveToLayout(panelId, e.TargetKind, e.SideHint);
     ```
   - Remove the handler when the child is detached (in
     `OnPanelRemoved` or the new `RemoveWithoutDisposing`).

4. **Add `MdiHost.RemoveWithoutDisposing(string id)`**:
   - Same as `Remove` but does NOT raise `ChildRemoved` and does NOT
     remove the child's parent's reference to the VM (the caller owns
     the VM lifecycle).
   - Used only by `MoveToLayout` to detach the visual without firing
     the panel-closed pipeline.

5. **Tests** under `tests/DdsMonitor.Avalonia.Tests/`:
   - `MoveToLayoutTests.cs`:
     - Spawn a panel as MDI, call `MoveToLayout(id, DockDocument)`,
       assert the dock manager's `AddDocument` was called, the MdiHost no
       longer contains it, and the VM instance is the same reference as
       before (use a recordable fake VM).
     - Repeat for `Mdi → DockTool` with sideHint "right" — assert the
       fake dock manager's `AddTool` received `DockSide.Right`.
     - `DockDocument → Mdi`: after the move, the MDI host contains the
       child at the geometry stored in `__window`.
     - Same VM instance throughout (no `Dispose` calls on the fake VM
       during the moves).
     - Workspace save after a `MoveToLayout` writes the new `LayoutKind`
       in the JSON.

**Implementation notes**:

- The "rebuild view from VM" step exists because Avalonia controls can
  only have one logical parent. The `IAvaloniaViewRegistry.BuildView`
  factory is idempotent for the same VM (it returns a new Control with
  the same `DataContext`).
- The guard set `_movingPanels` must be held only across the
  `Remove → Add` window, not the whole `MoveToLayout`. Use
  `try/finally` to ensure removal even if `Add` throws.

**Acceptance criteria**:

- [x] `dotnet build` succeeds with zero errors.
- [x] All `MoveToLayoutTests` pass.
- [x] Right-clicking an MDI child titlebar and selecting "Dock as tab"
  moves it to a Dock.NET tab; the VM state is preserved (verifiable
  manually: type into a SendSample text field, dock-as-tab, the text
  remains).
- [x] The reverse move (right-click a docked tab and selecting "Float
  in MDI" — UI added in M1-T16 below) returns the panel to the MDI
  area at the geometry it had before docking.
- [x] Workspace save and reload of a mixed layout (some MDI, some
  docked) round-trips correctly.

**Out of scope**:

- Drag-to-dock from MdiHost to Dock.NET (still deferred).

---

## M1-T15 — Live Dashboard Updates and Correct Menu Nesting

**Goal**: Make the FeatureDemo Dashboard actually display live counts,
and correctly nest the Devel menu items under a "Feature Demo" submenu.

**Depends on**: M1-T10.

**Inputs to read**:

- Current `FeatureDemoDashboardViewModel.cs` — note that `Tick()`
  replaces `_topicRows` with a new list and that the VM does not
  implement `INotifyPropertyChanged`.
- Current `FeatureDemoPlugin.cs` — note the menu registrations:
  `menuRegistry.AddMenuItem("Devel", "_Feature Demo Toggle Publisher", …)`.
- `DESIGN.md` §6 (Threading) for UI-thread rules.

**Deliverables**:

1. **Modify** `FeatureDemoDashboardViewModel.cs`:
   - Implement `INotifyPropertyChanged`.
   - Convert `TopicRows` from a replaced field into an
     `ObservableCollection<TopicCountRow>` (existing instance, mutated in
     place).
   - In `Tick()`, iterate the five topic types: if `TopicRows.Count == 5`,
     update existing rows in place via `TopicRows[i] = newRow`; if not
     yet populated, clear and add the five rows.
   - Make `PublisherStateLabel` raise `PropertyChanged` when
     `_publisher.IsPublishing` changes — subscribe to a new
     `event Action<bool>? PublishingChanged` on `DemoPublisherService`
     that fires from `StartPublishing` and `StopPublishing`. The VM
     subscribes in its constructor and unsubscribes on `Dispose`
     (add `IDisposable` to the VM).
   - Add `IDisposable` implementation: dispose the subscription.
   - `Tick()` must be safe to call on a background thread — guard the
     `ObservableCollection` mutations with an `IUiThreadInvoker.Post`
     when not on the UI thread. The view's `DispatcherTimer` ticks
     on the UI thread so this is defensive.

2. **Modify** `DemoPublisherService.cs`:
   - Add `event Action<bool>? PublishingChanged`.
   - Fire `PublishingChanged?.Invoke(_publishing)` at the end of
     `StartPublishing` and `StopPublishing`.

3. **Fix `Tick()` alert harvesting**:
   - The current code does `Samples.Skip(prev)` where `prev = RecentAlerts.Count`.
     This is wrong because `RecentAlerts` is capped at 10, so after the
     11th alert the skip count desynchronises.
   - Track the last consumed `Ordinal` in a private field
     `_lastAlertOrdinal = long.MinValue`. Each tick, walk
     `topicSamples.Samples` and consume any whose `Ordinal > _lastAlertOrdinal`,
     updating `_lastAlertOrdinal` to the last seen value.

4. **Modify** `FeatureDemoPlugin.cs`:
   - Replace:
     ```csharp
     menuRegistry.AddMenuItem("Devel", "_Feature Demo Toggle Publisher", …)
     ```
     with nested-menu registration so the path is
     `Devel → _Feature Demo → _Toggle Publisher`.
   - Use whichever overload `IMenuRegistry` provides for nesting (e.g.
     `AddMenuItem("Devel", new[]{"_Feature Demo", "_Toggle Publisher"}, action)`,
     or `AddNestedMenuItem(parentPath, leafLabel, action)` — read the
     engine's `IMenuRegistry.cs` to find the correct API).
   - If the engine has no nesting API, add one (`AddMenuItem(string parent,
     IReadOnlyList<string> path, Func<Task>? onClick, Action? onClick = null)`)
     and update existing call sites.

5. **Tests**:
   - `FeatureDemoDashboardViewModelTests.cs` (update existing):
     - With a fake `ISampleStore` returning increasing counts on
       successive `GetTopicCount` calls, call `Tick()` twice and assert
       `PropertyChanged` fires for `TopicRows` (or row items, depending
       on collection model) at least once.
     - Toggle the publisher: assert `PropertyChanged` fires for
       `PublisherStateLabel`.
     - Replay 30 alerts through a fake sample store; assert
       `RecentAlerts.Count == 10` and that the last alert in the list
       is alert #30 (proves `_lastAlertOrdinal` correctness).

**Implementation notes**:

- An ObservableCollection of `TopicCountRow` records works because
  records are immutable — replacing the item in the collection fires
  the right notifications. Don't try to mutate the record in place.
- The publisher's `PublishingChanged` event fires on a background thread
  (from the publish loops' cancellation paths). The VM's handler must
  marshal to the UI thread via `IUiThreadInvoker.Post`.

**Acceptance criteria**:

- [x] `dotnet build` succeeds with zero errors.
- [x] Updated dashboard tests pass.
- [x] Running the app + opening View → Feature Demo Dashboard shows
  counts that increase at the spec'd rates within ~1 second of opening
  the panel.
- [x] Toggling the publisher updates the button label from "Publishing"
  to "Stopped" without re-opening the panel.
- [x] The Devel menu shows `Feature Demo ▶ Toggle Publisher` as a
  proper nested submenu, not a flat item.

**Out of scope**:

- Visualising the published values graphically (deferred).

---

## M1-T16 — Wire Up All Shell Menu Items and Fix Plugin-Menu Routing

**Goal**: Every menu item in `ShellWindow.axaml` has a working `Click`
handler — either a real implementation (Theme, Reset Layout, Export/
Import Layout, Exit, transport) or a placeholder dialog ("Coming in M*n*").
Plugin-contributed menu items go to the correct top-level menu based on
the `parent` argument given when they were registered, not all to
"Plugins".

**Depends on**: M1-T7 (Shell), M1-T8 (FileDialogService, ThemeService).

**Inputs to read**:

- Current `ShellWindow.axaml.cs` — note `RebuildPluginsMenu` reads
  `_menuRegistry.GetTopLevelMenus()` and dumps everything under the
  Plugins menu.
- `DdsMonitor.Engine.Plugins.IMenuRegistry` — read the signature of
  `GetTopLevelMenus()` and how nodes carry their parent.
- `DESIGN.md` §3 (Hybrid Layout Model) and §7 (Visual Design Tokens).

**Deliverables**:

1. **Rewrite `RebuildPluginsMenu` as `RebuildPluginMenus`**:
   - For each `MenuNode` from `_menuRegistry.GetTopLevelMenus()`:
     - Read the node's parent path (top-level name such as "View",
       "Devel", "Plugins"). If `IMenuRegistry` doesn't expose the parent,
       extend it: add a `string? ParentMenu` field to `MenuNode` and
       populate it from the `parent` argument passed to `AddMenuItem`.
     - Map "View" → `ViewMenu`, "Devel" → the Devel `MenuItem` (give it
       `x:Name="DevelMenu"` in XAML and update code-behind), "Plugins" →
       `PluginsMenu`, "File" → ignore (File menu is static).
     - Recursively build sub-items via the existing `BuildMenuItem`.
   - Static items in each menu (Theme submenu, transport, etc.) must
     not be removed during rebuild — clear only the plugin-contributed
     items by tagging them on creation. Simplest approach: track a
     `List<MenuItem> _pluginMenuItems` per parent and remove only those
     before re-adding.

2. **Wire the static File-menu items**:
   - `TopicSourcesItem.Click`: spawn `SchemaSourcesViewModel` via
     `IWindowManager.SpawnPanel(nameof(SchemaSourcesViewModel), null)`.
   - `PluginManagerItem.Click`: open placeholder dialog
     `await ShowComingInDialog("Plugin Manager", "M5")`.
   - `ResetLayoutItem.Click`: open a confirmation dialog "Reset layout?
     All open panels will be closed."; if confirmed,
     `IWindowManager.ClearPanels()` then `IDockManager.Initialise(...)` again
     with the same MDI host (this re-creates the default layout).
   - `ExportLayoutItem.Click`: use `IFileDialogService.SaveFileAsync(
     "Export Layout", "ddsmon-workspace.json", filters: [{ "JSON", ["json"] }])`,
     then `IAvaloniaWindowManager.SaveWorkspace(path)`.
   - `ImportLayoutItem.Click`: use `IFileDialogService.OpenFileAsync(...)`,
     then `IWindowManager.ClearPanels()` and `IWindowManager.LoadWorkspace(path)`.
   - `ExitItem.Click`: already wired in M1-T7. Confirm by inspection.

3. **Wire the Devel menu items**:
   - `SelfSendItem.Click` (toggle): call into `IDdsBridge` (or wherever
     the existing self-send service lives — see
     `Engine/Testing/SelfSendService.cs` for the contract) to toggle
     `SelfSendEnabled`. Make `SelfSendItem` an `MenuItem` with
     `IsCheckable=true` and bind/sync the `IsChecked` state.
   - `Rate1HzItem`, `Rate10HzItem`, `Rate100HzItem`, `Rate1KHzItem`,
     `Rate10KHzItem`: each calls
     `selfSendService.SetRate(1)`, `(10)`, `(100)`, `(1000)`, `(10000)` Hz.
     Implement as a radio-group via `MenuItem.ToggleType="Radio"` if
     Avalonia supports it; otherwise visual mark via `IsChecked` toggling
     in code-behind on each click.
   - `PerfStatsItem.Click`: open placeholder dialog "Perf Stats — M5".

4. **Wire the Theme submenu visual feedback**:
   - When the user picks a theme, the corresponding `MenuItem` shows a
     check-mark and the others don't.
   - Subscribe to `IThemeService.ModeChanged` and update
     `ThemeSystemItem.IsChecked`, `ThemeLightItem.IsChecked`,
     `ThemeDarkItem.IsChecked` accordingly.
   - All three must be `IsCheckable=true` (in XAML).

5. **Add a `ShowComingInDialog` helper**:
   - Either a small custom `Window` or use `MessageBoxManager` from
     `MessageBox.Avalonia` if added as a package reference.
   - Recommend: write a 30-line `ComingInDialog : Window` with a
     `TextBlock` and a single OK button. No external package needed.

6. **Wire the right-click "Float in MDI" item on dock tabs** (counterpart
   to M1-T14's MDI titlebar context menu):
   - In `DockManager`, after adding a Document or Tool, attach a
     custom `ContextMenu` to the dockable's tab (Dock.NET supports a
     `Document.TabContextMenu` or equivalent — verify via Dock.NET docs).
   - Menu items: "Float in MDI" → `windowManager.MoveToLayout(id, LayoutKind.Mdi)`,
     "Close" → `dockManager.Remove(id)`.
   - If Dock.NET doesn't expose tab-level context menus in 11.2.x, add a
     hook on the document's `Context` (the `Control`) — wrap the user
     content in a `Border` that handles right-click. Document the
     chosen approach in a code comment.

7. **Tests**:
   - `ShellMenuTests.cs`:
     - With a fake `IMenuRegistry` registering items under "View",
       "Devel", and "Plugins", assert each appears in the correct
       top-level menu (not all in `PluginsMenu`).
     - Click each File menu item: assert the right side effect occurs
       (mock services).
     - Theme submenu: pick Light, assert `IThemeService.SetMode(Light)`
       was called and `ThemeLightItem.IsChecked == true`; the other two
       become false.
   - `ResetLayoutTests.cs`: after clicking Reset and confirming,
     assert `IWindowManager.ClearPanels` and `IDockManager.Initialise`
     were called in that order.

**Implementation notes**:

- The "MenuNode parent" capture in `IMenuRegistry` needs verification —
  the engine's existing `MenuNode` may already expose this. If not, the
  modification is a minimal addition.
- The `DevelMenu` `x:Name` needs to be added in `ShellWindow.axaml`
  (it currently has no `x:Name`).

**Acceptance criteria**:

- [x] `dotnet build` succeeds with zero errors.
- [x] All shell menu and reset tests pass.
- [x] Every menu item in the shell either performs a real action or
  shows a "Coming in M*n*" dialog. None do nothing on click.
- [x] FeatureDemoPlugin's "Feature Demo Dashboard" item appears under
  the **View** menu, not under Plugins.
- [x] Picking a Theme from the submenu updates the visual check-mark.
- [x] Right-clicking a docked tab shows "Float in MDI" / "Close";
  selecting "Float in MDI" returns the panel to the MDI area at its
  last-known geometry.

**Out of scope**:

- Per-plugin enable/disable in the Plugin Manager (M5).
- Perf-stats overlay (M5).

---

## M1-T17 — Live Bandwidth, Status Indicator, and Participant Indicator

**Goal**: The shell's right-side status area shows live data: bandwidth
in human-readable units, the running/paused dot in the correct token
colour, and a participant summary button that opens NetworkConfig on
click. Status updates are event-driven where possible (with a 1 Hz
fallback timer for derived rates).

**Depends on**: M1-T7 (Shell), M1-T8 (ThemeService).

**Inputs to read**:

- `DESIGN.md` §7 (Visual Design Tokens) — note the `Accent.Receiving`
  and `Accent.Paused` resource keys.
- Current `ShellWindow.axaml.cs` — note `BandwidthText.Text = FormatBandwidth(0)`
  and `Brushes.Orange` / `Brushes.Green` hardcoded.
- `IDdsBridge` interface — search for byte/bandwidth counters
  (e.g. `TotalBytesReceived`, `BytesPerSecond`).
- `Engine/IParticipantManager.cs` or similar — for the participant summary
  data source.

**Deliverables**:

1. **Bandwidth source**:
   - If `IDdsBridge` exposes `TotalBytesReceived` (a monotonic counter):
     compute `bytesPerSecond = (currTotal - lastTotal) / interval` in
     the 1 Hz tick.
   - If `IDdsBridge` exposes a precomputed rate: use it directly.
   - If neither: extend `IDdsBridge` with `long TotalBytesReceived { get; }`
     and update the implementation to track it (sum `SampleData.SizeBytes`
     for each received sample). This is a small Engine change — call it
     out in commit message.

2. **Status dot colour fix**:
   - In the 1 Hz `OnStatusTick`:
     ```
     StatusDot.Fill = _ddsBridge.IsPaused
         ? (IBrush)Application.Current!.FindResource("Accent.Paused")!
         : (IBrush)Application.Current!.FindResource("Accent.Receiving")!;
     ```
   - Re-resolve the brush on every tick so theme changes take effect
     immediately.

3. **Bottom status bar `Background`**:
   - Set the `Border DockPanel.Dock="Bottom"` Background to
     `{DynamicResource Surface.Titlebar}` and add a 1 px top border
     `BorderBrush="{DynamicResource Border.Subtle}" BorderThickness="0,1,0,0"`.

4. **Add participant indicator button** in the status area:
   - In `ShellWindow.axaml`, between the bandwidth `TextBlock` and the
     right edge:
     ```xml
     <Button x:Name="ParticipantButton"
             Content="Domain=0"
             Background="Transparent"
             BorderBrush="{DynamicResource Border.Subtle}"
             BorderThickness="1"
             Padding="6,2"
             Margin="8,0,0,0"
             ToolTip.Tip="Click to configure DDS participants"/>
     ```
   - In code-behind, on click:
     `_windowManager.SpawnPanel(nameof(NetworkConfigViewModel), null)`.
   - On the 1 Hz tick, update `ParticipantButton.Content` from the
     participant manager:
     ```
     ParticipantButton.Content = $"Domain={string.Join(",", domainIds)}";
     ```
     Truncate to 30 chars with ellipsis if longer.

5. **Update the 1 Hz tick handler** to do all of:
   - Status text + dot (uses dynamic-resource brushes per #2).
   - Bandwidth formatting using `FormatBandwidth`.
   - Participant button content.

6. **Subscribe to `IDdsBridge.PausedChanged` event** (if it exists) so
   the dot/text update *immediately* on pause toggle, not 1 Hz later.
   If the event doesn't exist, add it to the Engine (`event Action<bool>? IsPausedChanged`).

7. **Tests**:
   - `ShellStatusTests.cs`:
     - With a fake bridge whose `TotalBytesReceived` grows by 1 MB per
       call, after two ticks `BandwidthText.Text` ≈ "1.05 MB/s".
     - Toggle paused: status dot's `Fill` switches between the two brush
       resources.
     - Click the participant button: `SpawnPanel(nameof(NetworkConfigViewModel))` called.
     - Theme change at runtime → next tick re-resolves brushes and the
       dot's fill changes accordingly.

**Implementation notes**:

- `FormatBandwidth` already exists in `ShellWindow.axaml.cs` and handles
  the unit thresholds — reuse it.
- The 1 Hz timer is `DispatcherTimer` on the UI thread — no marshalling
  needed for the updates.

**Acceptance criteria**:

- [x] `dotnet build` succeeds with zero errors.
- [x] All `ShellStatusTests` pass.
- [x] Running the app with FeatureDemo on: BandwidthText shows a non-zero
  rate within 2 seconds.
- [x] Theme toggle changes the status dot colour without app restart.
- [x] Status dot updates within 200 ms of Space being pressed
  (event-driven if `PausedChanged` available; otherwise still 1 Hz max).
- [x] Participant button shows `Domain=0` (or similar) and clicking it
  opens NetworkConfig.

**Out of scope**:

- Per-domain throughput breakdown (M5 Perf Stats).
- Multiple-participant editing UI (the existing NetworkConfig view
  handles it).

---

## M1-T18 — MdiHost Geometry Correctness

**Goal**: Fix four geometry bugs in `MdiHost` and `AvaloniaWindowManager`:
the resize-from-left/top origin drift, the `ChildGeometryChanged` event
not firing at the right moment, the missing X/Y capture on close, and the
missing `IsMinimised` persistence.

**Depends on**: M1-T4 (MdiHost), M1-T5 (WindowManager).

**Inputs to read**:

- Current `MdiHost.cs` — especially `OnChildResizeRequested`, which has
  bugs in the Left and Top edge branches.
- Current `AvaloniaWindowManager.OnPanelRemoved` — note it captures
  width/height but not X/Y.
- `DESIGN.md` §5 (Workspace Persistence) — the `__window` schema.

**Deliverables**:

1. **Fix `MdiHost.OnChildResizeRequested` Left/Top branches**:
   - Current code (broken):
     ```csharp
     if (e.Edge.HasFlag(ResizeEdge.Left))
     {
         var proposed = width - e.DeltaX;
         var clamped  = Math.Max(MinWidth, proposed);
         width  = clamped;
         left  += width != clamped ? 0 : e.DeltaX; // wrong: width == clamped here
     }
     ```
   - Correct logic: when the user drags the left edge by `dx`:
     - New proposed width = `oldWidth - dx`.
     - If proposed >= MinWidth: width = proposed, left += dx.
     - Else: width = MinWidth, left unchanged (or adjusted so right edge
       stays put, but spec is "do not move origin when clamped").
     - In code:
       ```csharp
       if (e.Edge.HasFlag(ResizeEdge.Left))
       {
           var proposed = width - e.DeltaX;
           if (proposed >= MinWidth) { width = proposed; left += e.DeltaX; }
           else                       { width = MinWidth;                  }
       }
       ```
     - Same pattern for Top.

2. **Fix `ChildGeometryChanged` timing**:
   - Currently fires on **every pointer-move** of resize. It should fire
     **once on pointer-release** for both drag and resize.
   - Pattern: in the `OnChildDragRequested` / `OnChildResizeRequested`
     handlers, set a `_pendingGeometryChange[childId] = true` flag.
   - Subscribe each added child's `PointerReleased` (capture-released)
     on the host side, and on release, if the pending flag is set, fire
     `ChildGeometryChanged` with the final bounds and clear the flag.
   - Alternative cleaner pattern: add `DragCompleted` and `ResizeCompleted`
     routed events on `MdiChild` that fire on `PointerReleased` after a
     drag/resize, and subscribe to those in `MdiHost`. The
     `ChildGeometryChanged` event fires from the host's
     `OnDragCompleted`/`OnResizeCompleted` once per gesture.

3. **WindowManager: capture X/Y on close**:
   - In `AvaloniaWindowManager.OnPanelRemoved`, when `child is not null`:
     ```csharp
     state.X = Canvas.GetLeft(child);
     state.Y = Canvas.GetTop(child);
     if (child.Width  > 0) state.Width  = child.Width;
     if (child.Height > 0) state.Height = child.Height;
     ```
   - Then write all four into `__window`.

4. **Persist `IsMinimised`**:
   - Extend the `__window` dictionary in `OnPanelRemoved`:
     ```csharp
     state.ComponentState["__window"] = new Dictionary<string, object>(...)
     {
         ["X"] = x, ["Y"] = y, ["Width"] = w, ["Height"] = h,
         ["IsMinimised"] = child?.IsMinimised ?? false,
     };
     ```
   - Read it back in `SpawnPanel` when restoring from saved state:
     ```csharp
     if (geoDict.TryGetValue("IsMinimised", out var im) && ToBool(im))
         _mdiHost.Minimise(panelState.PanelId);
     ```
   - Call `Minimise` *after* `Add` so the child is registered before
     it's hidden.

5. **Subscribe `AvaloniaWindowManager` to `MdiHost.ChildGeometryChanged`**:
   - In `SetMdiHost`, subscribe and publish `WorkspaceSaveRequestedEvent`
     on each event. The debounce handled by
     `AvaloniaWorkspacePersistenceService` collapses these into a single
     save.
   - Also update the in-memory `PanelState.X/Y/Width/Height` so a
     subsequent in-process workspace save reflects current geometry
     (currently those values are stale until close).

6. **Tests**:
   - `MdiHostResizeTests.cs` (extends M1-T4 tests):
     - Resize a child from the LEFT by +50 px (i.e. drag handle right
       50 px shrinking the child): left += 50, width -= 50.
     - Resize from the LEFT by -1000 px (would shrink below MinWidth):
       width clamps to MinWidth, **left unchanged**.
     - Same scenarios for the TOP edge.
   - `MdiHostGeometryEventTests.cs`:
     - Drag a child 50 px right: `ChildGeometryChanged` fires exactly
       once on pointer release with the final bounds, not on each move.
     - Resize a child 30 px wider: same — one event on release.
   - `AvaloniaWindowManagerCloseTests.cs`:
     - Drag a child to (200, 150), close it: the saved workspace JSON's
       `__window` contains `X=200, Y=150`.
     - Minimise a child, save workspace, reload: child is re-spawned in
       minimised state (visible in the strip, not on the canvas).

**Implementation notes**:

- The "fire once on PointerReleased" requires that the host knows when
  the gesture ends. The cleanest way is to add the two new routed events
  on `MdiChild` (`DragCompleted`, `ResizeCompleted`) — the existing
  `PointerReleased` handlers in `MdiChild` are the right place to raise
  them.

**Acceptance criteria**:

- [x] `dotnet build` succeeds with zero errors.
- [x] All four named test files pass.
- [x] Visual smoke test: drag the left edge of an MDI child past its
  minimum width — the child stops shrinking but does not drift right.
- [x] Closing and reopening a panel restores its exact last-known
  position, size, and minimise state.

**Out of scope**:

- Animated transitions on minimise/restore (still deferred).
- Multi-monitor floating windows.

---

## M1-T19 — Visual Polish: Tokens, Strip, Empty Docks

**Goal**: Three small visual fixes. The status indicator and minimised
strip use design tokens correctly, and empty Dock.NET side docks
collapse to zero width/height.

**Depends on**: M1-T2 (Design Tokens), M1-T4 (MdiHost), M1-T6 (DockManager).

**Inputs to read**:

- Current `MdiHost.axaml` — note `Border` around the strip is always
  visible at `Height=32`.
- Current `DdsDockFactory.cs` — note empty side tool docks are added
  with `VisibleDockables = CreateList<IDockable>()` (empty).
- `DESIGN.md` §7 (Visual Design Tokens) — the token list.

**Deliverables**:

1. **MdiHost strip auto-hide**:
   - Wrap the strip `Border` in a binding: `IsVisible="{Binding ElementName=PART_MinimisedStrip, Path=IsVisible}"` — the inner `ItemsControl` already toggles `IsVisible` based on item count. Now the outer `Border` follows.
   - Alternative cleaner: set the outer Border's `IsVisible` from the
     `UpdateStripVisibility()` method directly (track a template part for the Border too — `PART_MinimisedStripBorder`).

2. **Dock.NET empty-side collapse**:
   - Dock.NET 11.2's behaviour: a `ToolDock` with an empty
     `VisibleDockables` collection should not occupy space when its
     parent `ProportionalDock` recomputes. Verify by experiment — if it
     does occupy space (which the audit suggests it does), add
     `Proportion = 0` on creation and switch to `Proportion = 0.2` when
     the first tool is added.
   - In `DdsDockFactory.CreateLayout`, set:
     ```csharp
     LeftToolDock.Proportion   = 0;
     RightToolDock.Proportion  = 0;
     BottomToolDock.Proportion = 0;
     ```
   - In `DockManager.AddTool`, when adding the first tool to a side that
     was previously empty: `targetDock.Proportion = side switch { Bottom => 0.25, _ => 0.2 }`.
   - When removing the last tool: set `Proportion = 0` again.

3. **Status dot uses dynamic resource** — covered by M1-T17 already, but
   verify after T17 lands.

4. **Audit `BlazorTypeDrawerAdapter` removal** — the legacy adapter from
   v10 is presumably unused; verify it has been removed from the Avalonia
   StandardPlugin csproj's compile items. If still present, delete it.
   Add as part of this task.

5. **Tests** (small):
   - `MdiHostStripTests.cs`:
     - Empty host: outer strip border has `IsVisible == false`.
     - Minimise one child: outer strip border becomes visible.
     - Restore: hidden again.
   - `DockEmptyCollapseTests.cs`:
     - Initialise dock with no tools: assert `LeftToolDock.Proportion == 0`.
     - Add a tool: assert `LeftToolDock.Proportion > 0`.
     - Remove the only tool: assert `LeftToolDock.Proportion == 0` again.

**Acceptance criteria**:

- [x] `dotnet build` succeeds with zero errors.
- [x] Visual: launching the app shows no visible left/right/bottom
  dock bars when no tools are docked.
- [x] Minimised strip is invisible (not just empty) when no children are
  minimised.
- [x] All polish-related tests pass.

**Out of scope**:

- Custom dock-area chrome (Dock.NET's defaults are kept).

---

## M1-T20 — Dock.NET Layout Serialisation

**Goal**: Persist and restore the full Dock.NET layout (splitter
proportions, document/tool order, per-side membership) across app
restarts. Current `DockManager.SerialiseLayout` stores only id/type/side,
losing splitter proportions; `DeserialiseLayout` is a no-op stub.

**Depends on**: M1-T6 (DockManager), M1-T18 (geometry capture established).

**Inputs to read**:

- Current `DockManager.cs` `SerialiseLayout` and `DeserialiseLayout`.
- Dock.NET 11.2 documentation on `DockSerializer` / `IFactory.SaveLayout`
  (the exact method name may vary by version — verify).
- `DESIGN.md` §5 (Workspace Persistence) — the `DockLayout` field
  contract.

**Deliverables**:

1. **Use Dock.NET's built-in serialiser** if it exists in 11.2.x:
   - Add `Dock.Serializer.Newtonsoft` or `Dock.Serializer.SystemTextJson`
     to the shell's `.csproj` (whichever is available — prefer
     System.Text.Json to match the rest of the codebase).
   - `SerialiseLayout()` returns
     `_serializer.Serialize(dockControl.Layout)`.
   - `DeserialiseLayout(json)`:
     - Deserialise into an `IRootDock`.
     - Assign to the live `DockControl.Layout`.
     - **Re-bind document/tool content** — Dock.NET serialises
       `Document.Title` / `Document.Id` but `Context` is typically not
       serialisable. After deserialisation, walk the loaded layout and
       for each `Document`/`Tool` with a known id, look up its
       `Control` content via a callback the WindowManager provides:
       ```csharp
       _dockManager.RehydrateContent(panelId => GetContentForPanel(panelId));
       ```

2. **If Dock.NET's serialiser is unavailable or unsuitable**, fall back
   to a manual DTO approach:
   - Define a `DockLayoutDto`:
     ```csharp
     internal sealed record DockLayoutDto(
         DockNodeDto Root,
         double LeftProportion,
         double RightProportion,
         double BottomProportion);

     internal abstract record DockNodeDto;
     internal sealed record ProportionalDockDto(
         string Orientation,
         IReadOnlyList<DockNodeDto> Children,
         IReadOnlyList<double> Proportions) : DockNodeDto;
     internal sealed record DocumentDockDto(
         IReadOnlyList<DockableDto> Documents,
         string? ActiveId) : DockNodeDto;
     internal sealed record ToolDockDto(
         string Side,
         IReadOnlyList<DockableDto> Tools,
         string? ActiveId,
         double Proportion) : DockNodeDto;
     internal sealed record DockableDto(string Id, string Title);
     ```
   - `SerialiseLayout()` walks the Dock.NET layout root, builds the DTO,
     and JSON-serialises.
   - `DeserialiseLayout()` rebuilds the layout structure (recreating
     `ProportionalDock`, `DocumentDock`, `ToolDock` instances with the
     saved proportions and the central MDI workspace document at its
     id) and asks the WindowManager to rehydrate content via a callback.

3. **Update `AvaloniaWindowManager.LoadWorkspaceFromJson`** to:
   - First spawn all panels (this populates the WindowManager's view +
     VM map and creates contents).
   - Then call `_dockManager.DeserialiseLayout(dockLayoutJson)`, passing
     a callback `panelId => _viewRegistry.BuildView(_viewModels[panelId])`.
   - Dock.NET binds the deserialised dockables to the rebuilt content.

4. **Update `AvaloniaWindowManager.SaveWorkspaceToJson`**: the call
   `_dockManager.SerialiseLayout()` returns full layout now; no other
   changes needed.

5. **Tests**:
   - `DockSerialisationTests.cs`:
     - Initialise a dock + add 2 documents to the centre and 1 tool to
       the left + 1 to the bottom. Drag the left splitter to 0.3
       proportion (programmatically via setting `Proportion`).
     - `SerialiseLayout()` → re-Initialise a fresh `DockManager` →
       `DeserialiseLayout(saved)` with a content-rehydrate callback.
     - Assert: documents and tools are in their original positions,
       `LeftToolDock.Proportion == 0.3`, the central MDI workspace is
       still present and non-closable.
   - `WorkspaceFullRoundtripTests.cs`:
     - Spawn 3 MDI panels + 2 docked tabs + 1 left tool, with various
       text-field values in fake VMs.
     - `SaveWorkspaceToJson()` → discard the WindowManager → fresh DI
       graph → `LoadWorkspaceFromJson(saved)`.
     - Assert: identical panel set, identical geometries, identical
       `LayoutKind`s, identical dock proportions, VM text fields restored.

**Implementation notes**:

- The chosen approach (built-in vs manual DTO) is up to the agent based
  on what Dock.NET 11.2 provides. Document the choice in a code comment.
- Content rehydration is the trickiest part: Dock.NET keeps a
  `Document.Context` reference to the live `Control`. After
  deserialisation, those references are null — the WindowManager must
  populate them by id.

**Acceptance criteria**:

- [x] `dotnet build` succeeds with zero errors.
- [x] Both serialisation test files pass.
- [x] Manual: open the app, dock a tool to the left, drag the splitter
  to ~30%, close the app, reopen — the tool is still docked with the
  same proportion.
- [x] Blazor-format workspace files (no `DockLayout` key) still load
  with the default dock layout — backward-compat test from M1-T5 still
  passes.

**Out of scope**:

- Floating Dock.NET windows persistence (out-of-process float feature).

---

## Milestones M2–M7

Detailed task lists for milestones M2 through M7 will be written when each
milestone is approached. The tracker contains one-line placeholders so the
user can see overall progress.

End of TASK-DETAILS.md
