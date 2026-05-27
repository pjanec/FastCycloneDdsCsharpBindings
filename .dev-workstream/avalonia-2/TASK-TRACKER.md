# Task Tracker

Tick the box when **all** acceptance criteria in the linked task pass.
Tasks must be done in dependency order — see each task's "Depends on"
field in [TASK-DETAILS.md](TASK-DETAILS.md).

Reference: [DESIGN.md](DESIGN.md) — shared architecture spec.

---

## Milestone M1 — Hybrid Shell, MDI Host, FeatureDemo

Foundation work. Establishes the Dock + MDI layout, design tokens, all
shared services, fixes existing bugs, and brings up the FeatureDemo
plugin as an end-to-end smoke test.

- [x] **M1-T0** — Preflight: solution skeleton, shared assemblies, Dock.NET package refs, FeatureDemo project skeleton, test project skeletons. → [details](TASK-DETAILS.md#m1-t0--preflight-solution-skeleton-and-shared-assemblies)
- [x] **M1-T1** — Avalonia.Core abstractions: `LayoutKind`, service interfaces (`IContextMenuPresenter`, `IFileDialogService`, `IKeyboardShortcutService`, `IThemeService`, `IClipboardService`, `IUiThreadInvoker`), `ToolbarEntry` extension. → [details](TASK-DETAILS.md#m1-t1--avaloniacore-abstractions)
- [x] **M1-T2** — Design tokens and base styles (`DesignTokens.axaml`, `BaseStyles.axaml`, `App.axaml` rewrite). → [details](TASK-DETAILS.md#m1-t2--design-tokens-and-base-styles)
- [x] **M1-T3** — `MdiChild` custom control: titlebar, eight resize handles, drag, z-order, focus, layout-kind context menu. → [details](TASK-DETAILS.md#m1-t3--mdichild-custom-control)
- [x] **M1-T4** — `MdiHost` custom control: canvas, z-order management, minimised strip, boundary clamping. → [details](TASK-DETAILS.md#m1-t4--mdihost-custom-control)
- [x] **M1-T5** — New `AvaloniaWindowManager` orchestrating MDI + Dock documents + Dock tools; workspace round-trip including Blazor-compat. → [details](TASK-DETAILS.md#m1-t5--new-avaloniawindowmanager)
- [x] **M1-T6** — Dock.NET integration: `IDockManager` + `DockManager` + `DdsDockFactory`, hybrid layout root with non-closable central document hosting the MDI host. → [details](TASK-DETAILS.md#m1-t6--docknet-integration)
- [x] **M1-T7** — `ShellWindow` rebuild: menus with mnemonics, transport buttons, bandwidth/participant indicators, status bar, dock+MDI composition, key bindings. → [details](TASK-DETAILS.md#m1-t7--shellwindow-and-app-composition)
- [x] **M1-T8** — Service implementations: `ContextMenuPresenter`, `FileDialogService`, `KeyboardShortcutService`, `ThemeService`, `ClipboardService`. → [details](TASK-DETAILS.md#m1-t8--service-implementations)
- [x] **M1-T9** — `Program.cs` rewrite, DI graph for all new services, fix the four DESIGN §13 bugs that aren't owned by other M1 tasks. → [details](TASK-DETAILS.md#m1-t9--programcs-di-and-bug-fixes)
- [x] **M1-T10** — `DdsMonitor.Avalonia.FeatureDemoPlugin`: five demo topic types, hosted publisher service, dashboard panel — the smoke-test bedrock. → [details](TASK-DETAILS.md#m1-t10--featuredemo-plugin)
- [x] **M1-T11** — `DdsMonitor.Avalonia.StandardPlugin` touch-ups: toolbar signature, menu mnemonics, no auto-spawn. → [details](TASK-DETAILS.md#m1-t11--standardplugin-touch-ups)
- [x] **M1-T12** — M1 end-to-end smoke test + manual test checklist. → [details](TASK-DETAILS.md#m1-t12--m1-end-to-end-smoke-test)

---

### M1 Finish-Up (added after post-M1 source audit)

The first pass left functional gaps that block M1 acceptance and would
compound through M2. These tasks close them. See task details for the
full audit findings each task addresses.

- [ ] **M1-T13** — Install keyboard shortcuts as actual `KeyBinding`s on `ShellWindow`; wire Ctrl+W, Ctrl+1..9, Ctrl+Tab, Ctrl+Shift+T, Ctrl+,, plus reopen-last-closed stack. → [details](TASK-DETAILS.md#m1-t13--install-keyboard-shortcuts-on-the-shell-window)
- [ ] **M1-T14** — Implement `MoveToLayout` and wire `MdiChild.LayoutKindRequested`; make "Dock as tab / left / right / bottom" actually work, with Dock-tab → MDI reverse via tab context menu. → [details](TASK-DETAILS.md#m1-t14--implement-movetolayout-and-wire-the-titlebar-context-menu)
- [ ] **M1-T15** — `FeatureDemoDashboardViewModel` `INotifyPropertyChanged`; correct `Devel → Feature Demo → Toggle Publisher` nesting; fix alert-harvest ordinal tracking. → [details](TASK-DETAILS.md#m1-t15--live-dashboard-updates-and-correct-menu-nesting)
- [ ] **M1-T16** — Wire every shell menu item (real action or "Coming in M*n*" placeholder); fix plugin-menu routing so View/Devel items go to the correct top-level menu; theme submenu visual feedback. → [details](TASK-DETAILS.md#m1-t16--wire-up-all-shell-menu-items-and-fix-plugin-menu-routing)
- [ ] **M1-T17** — Live bandwidth from `IDdsBridge`; status dot uses `Accent.Receiving/Paused` design-token brushes; add participant indicator button that opens NetworkConfig; event-driven pause updates. → [details](TASK-DETAILS.md#m1-t17--live-bandwidth-status-indicator-and-participant-indicator)
- [ ] **M1-T18** — Fix MdiHost resize-from-Left/Top origin drift; fire `ChildGeometryChanged` once on PointerReleased for both drag and resize; capture X/Y on close; persist `IsMinimised`. → [details](TASK-DETAILS.md#m1-t18--mdihost-geometry-correctness)
- [ ] **M1-T19** — Minimised-strip border auto-hides; empty Dock.NET side docks collapse to zero proportion; remove dead `BlazorTypeDrawerAdapter` if still present. → [details](TASK-DETAILS.md#m1-t19--visual-polish-tokens-strip-empty-docks)
- [ ] **M1-T20** — Full Dock.NET layout serialisation (splitter proportions, document/tool order); content rehydration callback in `LoadWorkspaceFromJson`. → [details](TASK-DETAILS.md#m1-t20--docknet-layout-serialisation)

---

## Milestone M2 — TopicExplorer Parity *(tasks TBD)*

Bring `TopicExplorerPanel.razor` (755 LoC) feature parity: tri-state
filters, search, color-coded names, sparkline column, sample-count/
instance-count columns, subscribe-all, context menus, action buttons,
auto-subscribe-at-startup logic, "no topic models" empty state.

- [ ] Task list to be written when M1 is complete and approved.

---

## Milestone M3 — SamplesViewer + InstancesPanel *(tasks TBD)*

The two heaviest panels. SamplesViewer = `SamplesPanel.razor` (2017 LoC):
virtualised DataGrid, column picker, per-field columns, value formatters,
follow-tail, per-panel pause, row selection → DetailInspector linking,
export to CSV/JSON. InstancesPanel = `InstancesPanel.razor` (1488 LoC):
keyed-topic instances grid with lifecycle coloring, transitions log.
Includes `ColumnPickerDialog`.

- [ ] Task list to be written when M2 is complete and approved.

---

## Milestone M4 — DetailInspector + TextView + TopicProperties *(tasks TBD)*

`DetailPanel.razor` (1412 LoC) parity: real `TreeDataGrid` field tree,
value formatter registry consumption, tooltip provider chain, union/
discriminator-aware rendering, clone-to-send action.
`TextViewPanel.razor` (109 LoC): JSON-syntax-highlighted sample dump.
`TopicPropertiesPanel.razor` (312 LoC): QoS attributes inspector.

- [ ] Task list to be written when M3 is complete and approved.

---

## Milestone M5 — FilterBuilder + Replay + PluginManager *(tasks TBD)*

`FilterBuilderPanel.razor` (656 LoC): visual filter editor over
`FilterNodes`. `ReplayPanel.razor` (520 LoC): record/playback transport.
`PluginManagerPanel.razor` (89 LoC): discovered plugins + enable/disable.
Plus Ctrl+Shift+P quick-open palette.

- [ ] Task list to be written when M4 is complete and approved.

---

## Milestone M6 — Reusable Controls + Polish *(tasks TBD)*

`FieldPicker.razor` (172 LoC), `TopicPicker.razor` (157 LoC),
`DynamicForm.razor` (605 LoC), tooltip portal + provider integration,
remaining drawer types (`enum`, `DateTime`, `Guid`, `FixedString32/64/128/256`),
animation polish.

- [ ] Task list to be written when M5 is complete and approved.

---

## Milestone M7 — ECS Plugin Port *(tasks TBD)*

Port `DdsMonitor.Plugins.ECS` from Blazor to Avalonia: entity grid panel,
entity-detail panel, settings panel, time-travel engine UI. Existing
engine-side ECS state model (`EntityStore`, `EntityHistoricalState`,
`TimeTravelEngine`) is reused.

- [ ] Task list to be written when M6 is complete and approved.

---

End of TASK-TRACKER.md
