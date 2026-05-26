# BATCH-01 Review

**Reviewer:** Dev Lead  
**Batch:** BATCH-01 — Foundation (M1-T0 + M1-T1)  
**Status:** ✅ APPROVED with P2 debt noted  
**Date:** 2026-05-26

---

## Decision: APPROVED

BATCH-01 is approved. Both M1-T0 and M1-T1 deliverables meet their acceptance
criteria. Build is clean (0 errors), core tests are 27/27 green, and the
FeatureDemoPlugin DLL is correctly staged. One pre-existing test failure was
uncovered (P2 debt — see below).

---

## Verification Results

| Check | Result |
|---|---|
| `dotnet build CycloneDDS.NET.sln -c Debug` | ✅ 0 errors, 1 pre-existing warning (bunit version) |
| `dotnet test DdsMonitor.Avalonia.Core.Tests` | ✅ 27/27 passed |
| `dotnet test DdsMonitor.Avalonia.FeatureDemoPlugin.Tests` | ✅ 1/1 passed |
| `dotnet test DdsMonitor.Avalonia.Tests` | ⚠️ 41/42 passed — 1 pre-existing failure |
| StagePlugin DLL present | ✅ True |

---

## Task Coverage

### M1-T0 — Preflight
- ✅ `Dock.Avalonia 11.2.0` and `Dock.Model.Mvvm 11.2.0` added to shell csproj.
- ✅ `DdsMonitor.Avalonia.FeatureDemoPlugin` project created with correct `StagePlugin`
  target, mirroring StandardPlugin csproj structure.
- ✅ `Placeholder.cs` stub created (to be replaced in M1-T10).
- ✅ `DdsMonitor.Avalonia.FeatureDemoPlugin.Tests` project created.
- ✅ Solution file updated: new plugin, test projects, and previously-orphaned
  `DdsMonitor.Avalonia.Tests` and `DdsMonitor.Avalonia.Core.Tests` all added with
  correct solution-folder nesting and build configs.
- ✅ `PluginLoader.SharedAssemblyNames` already contains `"DdsMonitor.Avalonia.Core"` —
  confirmed, no change needed.

### M1-T1 — Core Abstractions
- ✅ `LayoutKind` enum (Mdi, DockDocument, DockTool) created.
- ✅ `ToolbarEntry` updated with `Label` field; field order: `Id`, `Action`, `Label`,
  `Tooltip`, `IconKey?`.
- ✅ `IToolbarRegistry.Register` and `ToolbarRegistry` implementation updated with
  new signature (label added, iconKey moved to end).
- ✅ All six service interfaces present with correct signatures:
  `IContextMenuPresenter`, `IFileDialogService` (+ `FilePickerFilter`),
  `IKeyboardShortcutService` (+ `RegisteredShortcut`), `IThemeService` (+ `ThemeMode`),
  `IClipboardService`, `IUiThreadInvoker` (+ `AvaloniaUiThreadInvoker`).
- ✅ `IEventBrokerExtensions` updated with new `IUiThreadInvoker` overload alongside
  the existing `Dispatcher?` overload.
- ✅ No Dock.NET or shell references in `DdsMonitor.Avalonia.Core`.

---

## Test Quality Assessment

**Overall: GOOD**

Tests check actual behavior, not just compilation. Assertions are meaningful.

### ToolbarRegistryTests (6 tests — in AvaloniaCoreSuite.cs)
- `Register_SameId_ReplacesEntry`: ✅ asserts single entry AND the new action runs AND the new tooltip value is present. Good behavior test.
- `ChangedFires_OncePerRegistration`: ✅ counts the event fires — behavior, not "it didn't throw".
- `Entries_IsSnapshot`: ✅ verifies snapshot isolation by taking snapshot then adding more — correct.
- `Empty_EntriesIsEmpty`, `Register_NullId_Throws`, `RegisterTwo_BothInEntries`: ✅ all meaningful.

### IUiThreadInvokerTests (2 tests — new file)
- `Post_ExecutesAction`: ✅ verifies the posted action actually executed.
- `InvokeAsync_ExecutesFunc`: ✅ verifies async func completed.
- Uses `SynchronousInvoker` test stub that runs inline — correct pattern for deterministic tests.

### AvaloniaViewRegistryTests (3 tests — in AvaloniaCoreSuite.cs)
- `Register_BuildViewReturnsControl`: ✅ verifies BOTH the control type AND the data binding (`tb.Text == "Hi"`). Strong test.
- `UnregisteredType_ThrowsInvalidOperation`: ✅ correct exception type.
- `NullViewModel_ThrowsArgumentNull`: ✅ correct guard.

---

## Minor Issues Found

### Issue 1 — Test organisation (P3)
The batch spec asked for new dedicated test files (`ToolbarRegistryTests.cs`,
`AvaloniaViewRegistryTests.cs`). The developer instead added the new tests to the
existing `AvaloniaCoreSuite.cs`. The tests are real and correct, but the file is
growing large. **Not a blocker** — the tests pass and are quality.

### Issue 2 — ToolbarEntry stub arg drift in existing tests (P3)
In `AvaloniaCoreSuite.cs`, the call `registry.Register("btn1", () => { }, "icon1", "Tooltip 1")`
now maps `"icon1"` to `Label` (not `IconKey`) due to positional reorder. This is
semantically harmless since those tests only assert on `Id`, but it's a subtle
staleness. **Record in debt tracker — fix when touching that test file.**

---

## Pre-existing Bug Uncovered (P2 Debt)

**`PluginLoader_CorruptDll_DoesNotCrash` fails in Debug builds.**

This test existed on disk but was not in the solution before BATCH-01. The developer
correctly added the test project to the solution, making this pre-existing failure
visible for the first time.

**Root cause:** `PluginLoader.TryLoadPluginFromFile` wraps the exception catch in
`#if !DEBUG`, meaning in Debug builds, `BadImageFormatException` propagates uncaught.
This is an intentional "fail fast" pattern for development, but it breaks a test that
asserts corrupt DLLs don't crash the loader.

**Impact:** Test only fails in Debug; Release builds are correct. Production behavior
is unaffected.

**Resolution options (for BATCH-02 to assess):**
1. Remove the `#if !DEBUG` guards from `TryLoadPluginFromFile` (simplest fix) — but
   this makes debug sessions harder because plugin errors are silently swallowed.
2. Keep `#if !DEBUG` and add a separate `BadImageFormatException` catch that always
   applies (catches structural corruption but not logic errors).
3. Mark the test as `[Trait("Category", "ReleaseOnly")]` and configure CI to skip it
   in Debug — acknowledging the intentional Debug behavior.

**Recommendation:** Option 2 — always catch `BadImageFormatException` and
`FileLoadException` (structural DLL issues), but keep the broader `Exception` catch
only in non-Debug. This preserves the fail-fast design intent while fixing the test.

---

## Developer Insights Summary

- **Dock.NET**: 11.2.0 was the only available stable release for the 11.x branch. No
  version conflict with Avalonia 11.2.3.
- **TestApp missing `[assembly: AvaloniaTestApplication]`**: Critical fix. Without it,
  headless tests would fail with misleading errors. Well caught.
- **Solution orphans**: Both `DdsMonitor.Avalonia.Tests` and `DdsMonitor.Avalonia.Core.Tests`
  existed on disk but were not in the solution. Adding them revealed the pre-existing
  `PluginLoader_CorruptDll_DoesNotCrash` failure. Good detective work.

---

## Debt Tracker Updates

| ID | Priority | Description | Source | Target Batch |
|---|---|---|---|---|
| DEBT-001 | P2 | `PluginLoader_CorruptDll_DoesNotCrash` fails in Debug; `TryLoadPluginFromFile` should catch `BadImageFormatException` + `FileLoadException` always (not just in Release) | BATCH-01 review | BATCH-02 |
| DEBT-002 | P3 | `AvaloniaCoreSuite.cs` stub calls pass `"icon1"` to `Label` (not `IconKey`) after positional reorder | BATCH-01 review | When touching that test file |
| DEBT-003 | P3 | Test classes for `ToolbarRegistryTests` and `AvaloniaViewRegistryTests` should move to dedicated files as the suite file grows large | BATCH-01 review | When next touching test infra |

---

## Suggested Git Commit Message

```
feat: BATCH-01 — Foundation: Dock.NET packages, FeatureDemoPlugin skeleton, Core abstractions

M1-T0 (Preflight):
- Add Dock.Avalonia 11.2.0 + Dock.Model.Mvvm 11.2.0 to DdsMonitor.Avalonia.csproj
- Create DdsMonitor.Avalonia.FeatureDemoPlugin project with StagePlugin MSBuild target
- Create DdsMonitor.Avalonia.FeatureDemoPlugin.Tests skeleton
- Add all new + previously-orphaned projects to CycloneDDS.NET.sln

M1-T1 (Core Abstractions):
- Add LayoutKind enum (Mdi, DockDocument, DockTool)
- Add Label field to ToolbarEntry; update IToolbarRegistry.Register signature
- Add IContextMenuPresenter, IFileDialogService, IKeyboardShortcutService,
  IThemeService, IClipboardService, IUiThreadInvoker + AvaloniaUiThreadInvoker
- Add IUiThreadInvoker overload to IEventBrokerExtensions
- Add IUiThreadInvokerTests; extend ToolbarRegistryTests, AvaloniaViewRegistryTests
- Fix missing [assembly: AvaloniaTestApplication] in Core.Tests/TestApp.cs

Tests: 27/27 Core + 1/1 FeatureDemoPlugin green
```

---

## Task Tracker Updates

- [x] **M1-T0** — Preflight: solution skeleton, shared assemblies, Dock.NET package refs, FeatureDemo project skeleton, test project skeletons.
- [x] **M1-T1** — Avalonia.Core abstractions: `LayoutKind`, service interfaces, `ToolbarEntry` extension.
