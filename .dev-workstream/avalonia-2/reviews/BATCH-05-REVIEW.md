# BATCH-05 Review

**Reviewer:** Dev Lead  
**Date:** 2025-07-23  
**Tasks Reviewed:** M1-T11, M1-T10, M1-T12  
**Verdict:** APPROVED ✅ — all acceptance criteria met; M1 milestone complete

---

## Build & Test Results (verified by reviewer)

| Suite | Result |
|-------|--------|
| `CycloneDDS.NET.sln` Debug build | ✅ 0 errors |
| `DdsMonitor.Avalonia.Tests` | ✅ 98/98 passed |
| `DdsMonitor.Avalonia.Core.Tests` | ✅ 27/27 passed |
| `DdsMonitor.Avalonia.FeatureDemoPlugin.Tests` | ✅ 4/4 passed |
| `plugins/DdsMonitor.Avalonia.FeatureDemoPlugin.dll` staged | ✅ |
| `Placeholder.cs` deleted | ✅ |

---

## M1-T11 — StandardPlugin Touch-ups — APPROVED ✅

All 4 StandardPlugin files updated:

| File | Change |
|------|--------|
| `TopicExplorerPlugin.cs` | Menu text → `"_Topic Explorer"`, toolbar `label: "Explorer"` |
| `DummyDataGeneratorPlugin.cs` | Menu text → `"_Dummy Generator"` |
| `SendSamplePlugin.cs` | Menu text → `"_Send Sample"` |
| `WorkspaceManagerPlugin.cs` | `"Schema _Sources…"`, `"_Network Configuration…"` |

Auto-spawn audit: no plugin's `Initialize` method calls `SpawnPanel` directly. All `SpawnPanel` calls are in lambdas wired to menu click handlers or event broker subscriptions. ✅

---

## M1-T10 — FeatureDemoPlugin — APPROVED ✅

### DemoPublisherService
Modeled correctly on `DummyGeneratorService`. 5 independent publish loops, correct cancellation handling via `CancellationTokenSource`, `ToggleEnabled()` cleanly stops/starts. The `DemoPublisherServiceTests` verify both correct registration of 5 distinct topic types AND actual `Write` calls to the Telemetry writer (after 500ms with 100ms loop — at least 1 write). Second test verifies stop semantics: write count does not increase significantly after `ToggleEnabled()`. ✅

### DemoTypes.cs
Uses same `[DdsTopic]`, `[DdsKey]` attributes as `HeartbeatSample`. `UnionPayload` includes `[DdsUnion]` + `[DdsCase]` + `[DdsDiscriminator]` + `[DdsDefaultCase]`. ✅

### FeatureDemoDashboardViewModel
`Tick()` is correctly synchronous and called from code-behind's 1 Hz `DispatcherTimer`. Tests call `Tick()` directly — no timing dependency. `ObservableCollection<string>` for alerts is updated on the UI thread via the timer tick (code-behind is always on UI thread). ✅

### FeatureDemoPlugin.cs
- `ConfigureServices`: double-registers `DemoPublisherService` as singleton + hosted service (same instance via `sp.GetRequiredService<>` factory). ✅
- `Initialize`: registers view factory, 2 menu items. Does NOT auto-spawn dashboard on startup. ✅
- Menu mnemonics: `"_Feature Demo Toggle Publisher"` and `"Feature _Demo Dashboard"`. ✅

### FeatureDemoPlugin tests
2 publisher tests (behavioral, not tautologies), 2 ViewModel tests. `RecordingDdsBridge` properly implements all `IDdsBridge` members. ✅

---

## M1-T12 — E2E Smoke Tests + Manual Checklist — APPROVED ✅

### Smoke Test 1: Cold Start
Uses minimal DI (no engine services), asserts 4 menu items, empty `ActivePanels`, title "DDS Monitor". ✅

### Smoke Test 2: Five Topic Types
Uses `CapturingTopicRegistry` + `NoOpDdsBridge` (enabled=false so no publish loops started), asserts `StartAsync` registers exactly 5 distinct types. Clean and deterministic. ✅

### Smoke Test 3: Blazor JSON Round-trip
Uses `SmokeStubDockManager` + `SmokeStubServiceProvider` + real `MdiHost`. Loads Blazor-format JSON (no `LayoutKind` field) → asserts one panel in `ActivePanels`, one child in `MdiHost`. Saves and asserts `"LayoutKind"` and `"Mdi"` in output JSON. ✅

### Manual Checklist
29 items covering shell startup, theme switching, FeatureDemo plugin, MDI host behaviour, workspace round-trip, and StandardPlugin mnemonics. Comprehensive and actionable. ✅

---

## M1 Milestone Summary

All 12 M1 tasks accepted:

| Task | Status |
|------|--------|
| M1-T0 Preflight | ✅ |
| M1-T1 Core abstractions | ✅ |
| M1-T2 Design tokens | ✅ |
| M1-T3 MdiChild control | ✅ |
| M1-T4 MdiHost control | ✅ |
| M1-T5 AvaloniaWindowManager | ✅ |
| M1-T6 Dock.NET integration | ✅ |
| M1-T7 ShellWindow rebuild | ✅ |
| M1-T8 Service implementations | ✅ |
| M1-T9 Program.cs DI + bug fixes | ✅ |
| M1-T10 FeatureDemo plugin | ✅ |
| M1-T11 StandardPlugin touch-ups | ✅ |
| M1-T12 Smoke tests + checklist | ✅ |
| DEBT-001 PluginLoader fix | ✅ |

**Test totals:** 98/98 Avalonia.Tests · 27/27 Core.Tests · 4/4 FeatureDemoPlugin.Tests · 129 total

---

## Remaining Known Debt (carry to M2)

| ID | Priority | Description |
|----|----------|-------------|
| DEBT-002 | P3 | `AvaloniaCoreSuite.cs` stub `"icon1"` maps to `Label` not `IconKey` |
| DEBT-003 | P3 | Split `AvaloniaCoreSuite.cs` test classes to dedicated files |
| DEBT-004 | P2 | Pre-existing Engine.Tests failures (`PluginConfigService.HadConfigFileAtInitialization`) |
