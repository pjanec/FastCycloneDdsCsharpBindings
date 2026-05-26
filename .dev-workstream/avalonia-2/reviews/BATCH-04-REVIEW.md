# BATCH-04 Review

**Reviewer:** Dev Lead  
**Date:** 2025-07-23  
**Tasks Reviewed:** Corrective T0 (3 missing WindowManager tests), M1-T7, M1-T8, M1-T9  
**Verdict:** APPROVED ✅ — all acceptance criteria met; 2 P3 notes recorded

---

## Build & Test Results (verified by reviewer)

| Suite | Result |
|-------|--------|
| `CycloneDDS.NET.sln` Debug build | ✅ 0 errors |
| `DdsMonitor.Avalonia.Tests` (95) | ✅ 95/95 passed |
| `DdsMonitor.Avalonia.Core.Tests` (27) | ✅ 27/27 passed |

---

## Corrective T0 — APPROVED ✅

All 3 missing tests from BATCH-03 review are now present and behavioral:

| Test | Assertion | Result |
|------|-----------|--------|
| `LoadWorkspaceFromJson_BlazorFormat_AllMdi` | `mdiHost.Children.Count == 2` after loading Blazor JSON (no LayoutKind field) | ✅ |
| `ClosePanel_PublishesWorkspaceSaveRequestedEvent` | `RecordingEventBroker.published` contains `WorkspaceSaveRequestedEvent` after close | ✅ |
| `SaveAndLoad_RoundTrips_LayoutKind` | Saved JSON contains `"LayoutKind"`, `"Mdi"`, `"DockDocument"` tokens | ✅ |

`RecordingEventBroker` implementation is clean and minimal. M1-T5 fully accepted. ✅

---

## M1-T7 — ShellWindow Rebuild — APPROVED ✅

- New `ShellWindow.axaml`: `DockControl` fills the main content area; `MdiHost` lives inside the MDI Workspace document inside the dock. ✅
- 4-column top bar: menu (File/View/Devel/Plugins with all mnemonics), transport buttons (▶/⏸/⏹), status indicator. ✅
- Transport buttons have `ToolTip.Tip` set — bug §13.7 fixed. ✅
- Dead `ContentArea` Grid removed — bug §13.5 fixed. ✅
- `FormatBandwidth` exposed as `internal static` for direct unit testing. ✅
- `ShellWindowNewTests.cs`: 4 headless tests — menu item count, tooltip presence, bandwidth helper formatting, empty ActivePanels on start. ✅

**P3 note**: Space/F5 keyboard shortcut tests (toggle IsPaused, reset) were not included. These require a `IDdsBridge` stub and more complex event simulation; deferring to M1-T12 integration smoke test.

---

## M1-T8 — Service Implementations — APPROVED ✅

All 5 services implemented correctly:

| Service | Key behaviors verified |
|---------|----------------------|
| `ContextMenuPresenter` | Combines registry items + default items; `ContextMenu.Items.Count == 3` for 2 registry + 1 default |
| `FileDialogService` | Lazy `Func<Visual?>` provider; no call during construction |
| `KeyboardShortcutService` | Register, invoke, assert action ran; `Registered` contains the entry |
| `ThemeService` | `SetMode(Dark)` → `Application.RequestedThemeVariant == ThemeVariant.Dark`; `ModeChanged` fires |
| `ClipboardService` | Lazy `Func<TopLevel?>` provider; no call during construction |

9 service tests (4 keyboard + 3 theme + 2 context menu) — all behavioral. ✅

**P3 note**: Theme tests verify `RequestedThemeVariant` property change but cannot verify actual visual rendering in headless mode — documented and acceptable.

---

## M1-T9 — Program.cs DI + Bug Fixes — APPROVED ✅

All deliverables present and correct:

| Item | Status |
|------|--------|
| Double `BuildAvaloniaApp` call fixed — single call, result stored in `appBuilder` | ✅ |
| 7 new singletons registered: `IUiThreadInvoker`, `IContextMenuPresenter`, `IFileDialogService`, `IKeyboardShortcutService`, `IThemeService`, `IClipboardService`, `IDockManager` | ✅ |
| `IWindowManager` registered as factory → same `IAvaloniaWindowManager` instance | ✅ |
| Lazy providers for `FileDialogService` and `ClipboardService` (lambda captures `IClassicDesktopStyleApplicationLifetime`) | ✅ |
| Bug §13.3: `AvaloniaTypeDrawerRegistry.BuildFallback` passes `converted` (not `box.Text`) to `OnChange` | ✅ |
| Bug §13.4: `TopicExplorerViewModel` constructor no longer calls `RefreshTopics()` | ✅ |
| Bug §13.6: `TopicExplorerPlugin.Initialize` no longer auto-spawns; `StandardPluginSuite.cs` test updated | ✅ |
| `ProgramRegistrationTests`: all 9 services resolve; `IWindowManager === IAvaloniaWindowManager` | ✅ |

Non-obvious fix: `FormatBandwidth` uses `CultureInfo.InvariantCulture` to prevent locale-dependent decimal separator failures. Good defensive coding.

---

## Task Completion Status

| Task | Verdict |
|------|---------|
| Corrective T0 (BATCH-03) | ✅ Accepted |
| M1-T5 AvaloniaWindowManager (pending from BATCH-03) | ✅ Accepted — mark done |
| M1-T7 ShellWindow | ✅ Accepted — mark done |
| M1-T8 Service implementations | ✅ Accepted — mark done |
| M1-T9 Program.cs DI + bug fixes | ✅ Accepted — mark done |

---

## Notes for BATCH-05

- M1-T10 (FeatureDemo Plugin): delete `Placeholder.cs`, create 5 DDS topic types, `DemoPublisherService`, dashboard panel. Depends on M1-T9. ✅ All dependencies complete.
- M1-T11 (StandardPlugin touch-ups): toolbar Register signature, menu mnemonics, no auto-spawn (partially done — auto-spawn already removed in M1-T9).
- M1-T12 (E2E smoke test): `M1SmokeTest.cs` + `docs/M1-MANUAL-TEST-CHECKLIST.md`. Integration tests including Space/F5 keyboard shortcuts.
- No new debt items this batch.
