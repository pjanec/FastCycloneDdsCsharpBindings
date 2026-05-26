# DDS Monitor — Avalonia Port: Design Document

This document is the single source of truth for architectural decisions across
the entire Avalonia port. Individual task files (`TASK-DETAILS.md`) reference
sections here by anchor (e.g. `[see DESIGN.md#hybrid-layout-model]`) rather
than restating these rules.

> **Reading order**: skim this document end-to-end before starting any task.
> Then refer back per the cross-references in each task.

---

## 1. Scope and Goals

We are porting the existing Blazor-based DDS Monitor (`DdsMonitor.Blazor`) to
Avalonia 11 desktop. The Engine layer (`DdsMonitor.Engine`) is shared and must
not be modified except where explicitly noted as a task — it contains all DDS
plumbing, plugin loading, the event broker, and the workspace persistence
schema.

**The Avalonia port must reach 100% feature parity with the Blazor version**,
across all panels, dialogs, menus, plugins, and behaviours. The port is
delivered in seven milestones (M1–M7); this document covers the architecture
that all milestones share.

**Non-goals**:

- CSS visual parity with the Blazor app. We use the FluentTheme as the base
  and apply a restrained custom design-token layer (§7). No reproduction of
  `app.css`.
- A web target. Avalonia desktop only (Windows/Linux/macOS).
- API or schema changes to `DdsMonitor.Engine` beyond what is explicitly
  called out per task.

---

## 2. Solution Layout

Four Avalonia-side projects, all under `tools/DdsMonitor/`:

| Project | Role | References |
|---|---|---|
| `DdsMonitor.Avalonia.Core` | UI-toolkit-agnostic-ish abstractions: registries, services, value types | Engine only |
| `DdsMonitor.Avalonia` | Shell: main window, MDI host, Dock.NET integration, services, app composition | Engine + Avalonia.Core + Dock.NET |
| `DdsMonitor.Avalonia.StandardPlugin` | All standard panels (TopicExplorer, SamplesViewer, etc.) | Engine + Avalonia.Core |
| `DdsMonitor.Avalonia.FeatureDemoPlugin` | A rich diagnostic plugin used to smoke-test every panel end-to-end | Engine + Avalonia.Core |

`DdsMonitor.Avalonia.Core` references **Avalonia** (for `Control`, `Dispatcher`,
`Thickness`) but **must not** reference Dock.NET, Avalonia.Themes.Fluent, the
shell project, or any specific plugin. Plugins reference `Avalonia.Core` and
must not reference the shell or each other.

Plugins are loaded via the existing `PluginLoader` from the engine. The shell's
`.csproj` references plugin projects with `ReferenceOutputAssembly="false"` so
they build into the `plugins/` folder via the existing `StagePlugin` MSBuild
target.

---

## 3. Hybrid Layout Model {#hybrid-layout-model}

The user wants both docking and free-floating MDI children. The shell uses a
**hybrid layout**:

```
┌─ ShellWindow ──────────────────────────────────────────────┐
│ [File] [View] [Devel] [Plugins]   ▶ ⏸ ⏹    Running  N MB/s│  ← menu + transport + status
├────────────────────────────────────────────────────────────┤
│ ┌─ Dock.NET ProportionalDock ───────────────────────────┐  │
│ │ ┌─ Left tool dock ─┐ ┌─ Central DocumentDock ──────┐  │  │
│ │ │ TopicExplorer    │ │ ┌─ Document "MDI Workspace"─┐│  │  │
│ │ │ PluginManager    │ │ │ ┌───MdiChild────┐         ││  │  │
│ │ │ ...              │ │ │ │ Samples [T1]  │         ││  │  │
│ │ │                  │ │ │ └───────────────┘         ││  │  │
│ │ │                  │ │ │   ┌───MdiChild────┐       ││  │  │
│ │ │                  │ │ │   │ Detail        │       ││  │  │
│ │ │                  │ │ │   └───────────────┘       ││  │  │
│ │ │                  │ │ │ [minimised: Samples [T2]…]││  │  │
│ │ │                  │ │ └───────────────────────────┘│  │  │
│ │ └──────────────────┘ └─────────────────────────────┘   │  │
│ └────────────────────────────────────────────────────────┘  │
└────────────────────────────────────────────────────────────┘
```

- **Dock.NET** (`Dock.Avalonia` + `Dock.Model.Mvvm`, latest stable for Avalonia 11)
  provides the outer skeleton: tool panes on left/right/bottom and a central
  `DocumentDock` for tabbed documents.
- The **central DocumentDock** contains exactly one Document by default,
  titled "MDI Workspace", whose `Content` is a custom `MdiHost` canvas
  (§4). Additional documents can be created when users explicitly "Dock as
  tab" an MDI child.
- The **MdiHost** is a custom control we build (§4). It is the only place
  in the app where panels can overlap.

### Three `LayoutKind`s per panel

A panel can live in one of three places:

| `LayoutKind` | Where | How user gets it there |
|---|---|---|
| `Mdi` (default) | Inside the central `MdiHost` as a floating `MdiChild` | Default for every newly-spawned panel |
| `DockDocument` | A tab in the central `DocumentDock` | Right-click MDI titlebar → "Dock as tab" |
| `DockTool` | Docked tool pane on left/right/bottom | Right-click MDI titlebar → "Dock as left/right/bottom tool" |

Each panel can be moved between kinds via the titlebar context menu. The
current kind is persisted in the workspace JSON.

### First-launch behaviour

On first launch (no saved workspace file), the dock layout is **empty**: the
central `MdiHost` is visible but contains no children, all tool docks are
empty/collapsed. The user opens panels via the `View` menu; each opens as
a free-floating MDI child. They can then drag MDI children's titlebars to dock
them, or use the right-click "Dock as…" actions.

---

## 4. The Custom MdiHost {#mdi-host}

`MdiHost` is the most novel piece of code in the port. It lives in
`DdsMonitor.Avalonia` (not Avalonia.Core — it depends on too many Avalonia
controls).

### Visual structure

```
┌─ MdiHost (Border) ──────────────────────────────────────┐
│ ┌─ Canvas _childCanvas (fills, hosts MdiChild instances)┐│
│ │  ┌─MdiChild────┐  ┌─MdiChild────┐                    ││
│ │  │ titlebar    │  │ titlebar    │                    ││
│ │  │ ...content..│  │ ...content..│                    ││
│ │  └─────────────┘  └─────────────┘                    ││
│ └──────────────────────────────────────────────────────┘│
│ ┌─ DockPanel _minimisedStrip (Dock=Bottom) ────────────┐│
│ │ [Samples [T2]] [Detail] [Filters]                    ││  ← minimised children
│ └──────────────────────────────────────────────────────┘│
└────────────────────────────────────────────────────────┘
```

### MdiChild responsibilities

`MdiChild` is a templated `ContentControl` with:

- **Titlebar** with mnemonic-accelerated label, minimise (`_`), and close (`✕`)
  buttons. Right-click on the titlebar opens the layout context menu
  ("Dock as tab", "Dock as left/right/bottom tool", "Close").
- **Eight resize handles** (4 edges + 4 corners). The cursor hints at the edge
  using Avalonia's `StandardCursorType`. Resize respects `MinWidth`/`MinHeight`
  defaults of 220 × 140.
- **Drag** via the titlebar (left mouse button). Hold Shift while dragging to
  snap-grid to 8 px.
- **Z-order** via `Canvas.ZIndex`. Clicking anywhere inside an MdiChild raises
  it to front. Bringing a child to front sets its `Canvas.ZIndex` to
  `max(siblings) + 1`.
- **Boundary clamping**: when the host resizes smaller, all children are
  clamped so at least 40 px of their titlebar stays inside the host (lifted
  from `Desktop.razor`'s `EnsurePanelsVisibleAsync` logic).
- **Focus tracking**: an MdiChild that has focus inside it (via
  `IsKeyboardFocusWithin`) gets a 2 px accent-coloured border. Inactive
  children have a 1 px subtle border.

### State persisted to workspace

For an MDI child, the workspace JSON's `ComponentState["__window"]` contains
`{X, Y, Width, Height, IsMinimised}`. We extend the existing object schema with
`IsMinimised` (nullable bool, default false). Backward compat: omitting the
field means false, identical to the Blazor reading.

### Threading

All `MdiChild` mutations happen on the UI thread. The host subscribes to
`PanelsChanged` from `IWindowManager`; the manager dispatches that event to
`Dispatcher.UIThread`.

### Out of scope for `MdiHost` (do not add)

- Tabbing between MDI children inside the host (use Dock.NET for that).
- Multi-monitor floating windows (use Dock.NET float feature for that, in a
  later milestone).
- Animations beyond a 100 ms ease on minimise/restore. No drop shadows that
  follow drag (too expensive at 60 fps).

---

## 5. Workspace Persistence

The Blazor workspace JSON format must round-trip with the Avalonia version:

```jsonc
{
  "Panels": [
    {
      "PanelId": "...",
      "Title": "...",
      "ComponentTypeName": "DdsMonitor.Avalonia.StandardPlugin.SamplesViewerViewModel",
      "ComponentState": {
        "TopicName": "...",
        "FilterText": "...",
        "__window": { "X": 100, "Y": 50, "Width": 600, "Height": 400, "IsMinimised": false }
      },
      "LayoutKind": "Mdi"      // <-- new in Avalonia port
    }
  ],
  "ExcludedTopics": [ "..." ],
  "PluginSettings": { "...": {} },
  "DockLayout": { ... }        // <-- new: Dock.NET-serialised layout, opaque to engine
}
```

- **`LayoutKind`** is added at the panel level. Missing/null defaults to `Mdi`.
- **`DockLayout`** is a top-level field added by `AvaloniaWindowManager` that
  contains Dock.NET's serialised root layout. Missing → first-launch defaults
  apply (§3, empty central document with the MDI host).
- Everything else is the existing engine schema; do not break it.

When loading a Blazor workspace file:

- `LayoutKind` is missing → every panel becomes `Mdi`.
- `DockLayout` is missing → default tool-docks empty, single central
  document hosting the MDI host.
- Existing `__window` geometry is used to position MDI children.
- This produces an identical experience to the Blazor app's free-floating
  layout, fulfilling the "MDI by default" requirement.

The new `AvaloniaWindowManager` must implement `SaveWorkspaceToJson()` to emit
the same top-level fields the engine expects, plus `DockLayout`. Use the engine's
`WorkspaceSavingEvent` and `WorkspaceLoadedEvent` for plugin-defined sections,
exactly as the Blazor host does.

---

## 6. Threading and Async Conventions

- **Engine events fire on background threads.** Every UI handler **must**
  dispatch via `IEventBroker.SubscribeOnUiThread<T>()` (provided by
  `IEventBrokerExtensions` in `Avalonia.Core`).
- **ObservableCollection mutations** happen on the UI thread only.
- **DDS bridge writes** (sending samples, subscribing) may block briefly; do
  them on the UI thread for now — they are user-initiated and short.
- **No `async void`** outside event handlers. Use `async Task` and `_ = ...`
  to fire-and-forget if needed.
- **No `.Result` / `.Wait()`** anywhere; this stalls Avalonia's dispatcher.
- **`Task.Delay` debounces** use `CancellationTokenSource` exchange via
  `Interlocked.Exchange`, as the existing `AvaloniaWorkspacePersistenceService`
  does. That pattern is canonical for the port.

---

## 7. Visual Design Tokens {#design-tokens}

We base on FluentTheme but apply our own tokens via `App.axaml` resources.
The aesthetic is **calm, dense, instrumentation-style** — closer to a DAW or a
network analyser than to a consumer app. Restrained, no glow effects, no
gradients, no rounded chrome.

### Colour resources (light theme; dark variants in `ThemeVariantScope`)

| Token | Light | Dark | Used for |
|---|---|---|---|
| `Surface.Background` | `#FAFAFA` | `#1A1A1A` | Shell + MDI host background |
| `Surface.Panel` | `#FFFFFF` | `#242424` | MdiChild content background |
| `Surface.Titlebar` | `#F0F0F0` | `#2D2D2D` | MdiChild + dock titlebars |
| `Surface.TitlebarActive` | `#E5F1FB` | `#003E6B` | Focused MdiChild titlebar |
| `Border.Subtle` | `#D0D0D0` | `#3A3A3A` | Inactive child borders |
| `Border.Accent` | `#0078D4` | `#3399FF` | Focused child border, primary buttons |
| `Foreground.Primary` | `#1F1F1F` | `#F0F0F0` | Body text |
| `Foreground.Secondary` | `#6B6B6B` | `#9A9A9A` | Captions, metadata |
| `Accent.Receiving` | `#107C10` | `#6CCB5F` | Live/receiving indicator |
| `Accent.Paused` | `#D29922` | `#E0B341` | Paused indicator |
| `Accent.Error` | `#C42B1C` | `#FF6B6B` | Errors, send-failure banners |
| `Accent.Sparkline` | `#0078D4` | `#3399FF` | Sparkline stroke |

### Spacing and sizing

- Base unit: **4 px**. All margins/padding are multiples (4, 8, 12, 16, 24).
- MdiChild titlebar height: **28 px** fixed.
- Resize handle thickness: **6 px** on edges, **10 × 10** on corners.
- Minimised strip height: **32 px** when visible, collapsed otherwise.
- Toolbar button minimum hit area: **32 × 32** px.

### Typography

- All text uses `FontFamily="{StaticResource Inter}"` (already bundled by
  `Avalonia.Fonts.Inter` from the csproj).
- Default size **13 px** for body, **12 px** for captions, **11 px** for
  monospaced tabular data (timestamps, IDs, counts).
- Tabular data uses `FontFamily="Cascadia Mono, Consolas, monospace"`.
- No font weights beyond `Normal` and `SemiBold`. No italics except for
  empty-state placeholders.

### What this means in practice

- Buttons are flat with a 1 px border, no rounded corners larger than 2 px.
- Hover is a 5% lighten of the surface; no animation longer than 100 ms.
- No drop shadows on resting elements. MdiChildren get a 1 px border instead
  of a shadow.
- No icons in menus (use mnemonic underlines).
- Toolbar icons are SVG glyphs sized 16 px, monochrome, tinted via
  `Foreground.Primary`.

All tokens live in `App.axaml` and a `Themes/DesignTokens.axaml`
ResourceDictionary. Panels reference them by key with `{DynamicResource ...}`
so theme switching takes immediate effect.

---

## 8. Keyboard Navigation {#keyboard}

All shortcuts are wired through a `IKeyboardShortcutService` registered as
shell-level `KeyBinding`s in `ShellWindow.axaml.cs`.

### Global shortcuts

| Key | Action |
|---|---|
| `Alt` | Open menu bar (Avalonia native behaviour) |
| `Alt+F`, `Alt+V`, `Alt+D`, `Alt+P` | Open File, View, Devel, Plugins menus |
| `Space` | Toggle Play/Pause (`IDdsBridge.IsPaused`) |
| `F5` | Reset all data (`IDdsBridge.ResetAll`) |
| `Ctrl+W` | Close focused MDI child |
| `Ctrl+Shift+T` | Reopen last closed panel |
| `Ctrl+1`–`Ctrl+9` | Focus the *n*th MDI child (left-to-right titlebar order) |
| `Ctrl+,` | Open Plugin Manager |
| `Ctrl+Tab` / `Ctrl+Shift+Tab` | Cycle through MDI children |
| `Esc` | Close menu / cancel drag-resize / blur active textbox |
| `Ctrl+T` | Open new SamplesViewer for the topic selected in TopicExplorer |
| `Ctrl+Shift+P` | Quick-open palette (M5 — out of scope for M1) |

### Per-panel shortcuts (implemented in their respective milestones)

- `Ctrl+F` in any panel with a filter: focus the filter box.
- `Enter` on a focused row: invoke the row's default action.
- `Shift+F10` on a focused row: open the context menu at the row.
- `Esc` in a filter box: revert to the last valid filter and blur.

### Tab order rules

- Tab order in a panel follows reading order (top-to-bottom, left-to-right).
- Where the visual order is wrong (e.g. filter box should come before action
  buttons), set `TabIndex` explicitly in XAML.
- `IsTabStop="False"` for purely decorative elements (titlebar labels,
  sparkline SVGs).

### Mnemonics

- Every menu item and every primary button in a dialog has a mnemonic
  (underscored letter), set via `_` in the XAML `Header`/`Content`.
- Avoid mnemonic collisions within the same menu / dialog.

---

## 9. Services and Registries Reference {#services}

The shell registers the following singletons in `Program.cs`. Plugins access
them via `IMonitorContext.GetFeature<T>()`.

| Service | Defined in | Used for |
|---|---|---|
| `IMenuRegistry` | Engine | Plugin-contributed top-level menus |
| `IContextMenuRegistry` | Engine | Right-click items per data type |
| `IToolbarRegistry` | Avalonia.Core | Toolbar buttons |
| `IUserSettings` | Avalonia.Core | Per-user JSON-persisted preferences |
| `IAvaloniaViewRegistry` | Avalonia.Core | ViewModel → Control factory |
| `IAvaloniaTypeDrawerRegistry` | Avalonia.Core | CLR type → field editor Control |
| `IWindowManager` | Engine, impl in Avalonia | Spawn / close / dock panels |
| `IContextMenuPresenter` | Avalonia.Core | Render `IContextMenuRegistry` items as Avalonia `ContextMenu` |
| `IFileDialogService` | Avalonia.Core | OS file open/save (wraps `IStorageProvider`) |
| `IKeyboardShortcutService` | Avalonia.Core | Global hotkey table |
| `IThemeService` | Avalonia.Core | Light/dark/system toggle, persisted |
| `IClipboardService` | Avalonia.Core | Wraps `TopLevel.Clipboard` |
| `IEventBroker` | Engine | Cross-component messaging |

`IClipboardService` is a new abstraction added because the Blazor port used
`IJSRuntime` for clipboard work — that has no Avalonia equivalent, so it gets
its own service. Used by SamplesPanel "Copy as JSON" etc. in M3.

---

## 10. Plugin Loading and Asset Staging {#plugins}

The existing `PluginLoader` from the engine is used unchanged. Each plugin
project's `.csproj` contains the `StagePlugin` MSBuild target that copies
the built DLL into `tools/DdsMonitor/DdsMonitor.Avalonia/bin/$(Configuration)/$(TargetFramework)/plugins/`.

When adding a new plugin project, copy the `StagePlugin` target verbatim from
`DdsMonitor.Avalonia.StandardPlugin.csproj` and update the assembly name.

Shared assemblies (`DdsMonitor.Engine`, `DdsMonitor.Avalonia.Core`, etc.) must
be listed in `PluginLoader.SharedAssemblyNames` (in
`DdsMonitor.Engine/Plugins/PluginLoader.cs`). This is done in M1-T0.

---

## 11. Testing Strategy {#testing}

Each milestone delivers production code plus tests. Conventions:

- xUnit + `Avalonia.Headless.XUnit` for view-level tests where realistic.
- Pure ViewModel logic gets ViewModel tests with no Avalonia dependency
  (use `IUiThreadInvoker` instead of `Dispatcher.UIThread` directly so
  tests can use a synchronous invoker).
- Custom controls (`MdiChild`, `MdiHost`) get headless tests for drag,
  resize, z-order, minimise.
- Test project naming: `<ProjectName>.Tests`, located under `tests/`. The
  existing `DdsMonitor.Avalonia.Tests` project (referenced via
  `InternalsVisibleTo` in the Avalonia csproj) is the home for shell + MDI
  host tests.
- Coverage target: every public ViewModel method, every behaviour described
  in a task's Acceptance Criteria.
- A test is "passing" only if the headless run is green; no manual smoke
  tests count as acceptance.

---

## 12. Coding Conventions {#conventions}

- C# 12, target `net8.0`, `Nullable enable`, `ImplicitUsings enable`.
- File-scoped namespaces.
- `sealed` by default unless inheritance is part of the public contract.
- One public type per file. Internal helper types may co-locate.
- Doc comments on every public type and public member.
- `internal` for everything not crossing an assembly boundary.
- `readonly` fields; `init`-only properties where possible.
- No `var` for primitive types when the literal isn't obvious; otherwise
  prefer `var`.
- XAML: 2-space indent, attributes one-per-line when more than two.
- Resource keys use `Category.PascalCase` (e.g. `Surface.Background`,
  `Border.Accent`).
- Plugin classes are `sealed` and end in `Plugin` (e.g. `TopicExplorerPlugin`).
- ViewModels end in `ViewModel`, Views in `View`. The view's `DataContext`
  is always set to the ViewModel by the registered factory.

### Error handling

- Engine exceptions surface as user-visible banners — never silently swallowed
  beyond the existing patterns (`OperationCanceledException` in debounces,
  workspace-load corrupt-file recovery).
- ViewModel constructors don't perform I/O. I/O happens in `Initialize` (for
  `IStatefulViewModel`) or in an explicit `StartAsync` method.
- All `IDisposable` ViewModels dispose their subscription tokens and timers.
  An `MdiChild` close triggers `Dispose()` on its DataContext if the
  DataContext implements `IDisposable`.

---

## 13. Known Bugs to Fix in M1 {#known-bugs}

The existing port has these bugs; M1 fixes them inline rather than as a
separate cleanup pass:

1. **`ShellWindow.RebuildToolbar` sets `Button.Content = entry.Tooltip`** —
   the button shows the tooltip text *as* the button label. Correct behaviour:
   set `ToolTip.Tip` to `entry.Tooltip` and `Content` to a glyph derived from
   `entry.IconKey` (or `entry.Label` if no icon).
2. **`Program.cs` calls `BuildAvaloniaApp(host.Services)` twice**. The first
   result is discarded — only the `LogToTrace` side effect was intended. Fix
   by calling `BuildAvaloniaApp` once and reusing the builder.
3. **`AvaloniaTypeDrawerRegistry.BuildFallback` discards the converted value
   on `LostFocus`** — it calls `Convert.ChangeType` then passes `box.Text`
   (the string) to `ctx.OnChange`, not `converted`. Fix: pass `converted` on
   success, do nothing on conversion failure.
4. **`TopicExplorerViewModel.Initialize` calls `RefreshTopics()` after the
   constructor already did**. Move all state-dependent initialisation into
   `Initialize` and call `RefreshTopics()` exactly once.
5. **`ShellWindow.ContentArea` Grid is declared in XAML but never populated**.
   Remove it; the M1 shell rebuild replaces this region with the Dock.NET
   layout.
6. **`ToolbarEntry` has an unused `IconKey`** in addition to `Tooltip`. Add
   a `Label` field so we can show short text without forcing icons.
7. **Tool tips are wired with `ToolTip.Tip` on toolbar but missing on
   transport buttons.** Add tooltips to Play/Pause/Reset with the action
   they perform (referenced by `IKeyboardShortcutService`'s registered
   shortcut, e.g. `"Play (Space)"`).

These are listed individually in the corresponding M1 tasks.

---

## 14. Forward Compatibility Notes

Things that influence later milestones but should be designed-for in M1:

- The MDI titlebar must accept a `TitlebarExtras` slot (for per-panel buttons
  like "follow-tail" in SamplesViewer). M1 leaves it empty but exposes the
  attached property.
- `IClipboardService` is defined and registered in M1 but no panel uses it
  until M3.
- The `View` menu in M1 only contains "Topic Explorer" (placeholder for
  M2's full feature wiring), "Plugin Manager…" (M5), and "Feature Demo
  Dashboard" (a panel provided by the new FeatureDemo plugin). Real panel
  entries are wired by each milestone's plugin code, not hard-coded in the
  shell.

---

End of DESIGN.md
