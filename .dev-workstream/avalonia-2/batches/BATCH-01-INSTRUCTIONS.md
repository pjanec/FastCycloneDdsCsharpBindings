# BATCH-01: Foundation — Solution Skeleton & Core Abstractions

**Batch Number:** BATCH-01  
**Tasks:** M1-T0, M1-T1  
**Milestone:** M1 — Hybrid Shell, MDI Host, FeatureDemo  
**Estimated Effort:** 14–18 hours  
**Priority:** HIGH  
**Dependencies:** None (first batch)

---

## 📋 Onboarding & Workflow

### Developer Instructions

This is the first batch of the Avalonia-2 workstream. You are implementing the
foundation: project skeleton, Dock.NET package references, the FeatureDemoPlugin
project skeleton, and all the service interface abstractions in `DdsMonitor.Avalonia.Core`.

### Required Reading (IN ORDER)

1. **Workflow Guide:** `.github/skills/developer/SKILL.md` — how to work with batches.
2. **Design Document:** `.dev-workstream/avalonia-2/DESIGN.md` — read **end-to-end**
   before writing any code. Pay special attention to §2 (Solution Layout), §9 (Services),
   §10 (Plugin Loading), §11 (Testing Strategy), §12 (Coding Conventions), §13 (Known Bugs).
3. **Task Details:** `.dev-workstream/avalonia-2/TASK-DETAILS.md` — read the
   full specifications for **M1-T0** and **M1-T1**.
4. **Existing Core:** `tools/DdsMonitor/DdsMonitor.Avalonia.Core/` — read all files
   to understand what already exists before adding anything new.
5. **Existing Plugin:** `tools/DdsMonitor/DdsMonitor.Avalonia.StandardPlugin/DdsMonitor.Avalonia.StandardPlugin.csproj`
   — template for the new FeatureDemoPlugin csproj.
6. **PluginLoader:** `tools/DdsMonitor/DdsMonitor.Engine/Plugins/PluginLoader.cs`
   — check the `SharedAssemblyNames` HashSet.
7. **Solution file:** `CycloneDDS.NET.sln` — understand the existing project
   organisation and solution folders.

### Source Code Locations

- **Core abstractions:** `tools/DdsMonitor/DdsMonitor.Avalonia.Core/`
- **New plugin skeleton:** `tools/DdsMonitor/DdsMonitor.Avalonia.FeatureDemoPlugin/` *(create)*
- **Shell project:** `tools/DdsMonitor/DdsMonitor.Avalonia/DdsMonitor.Avalonia.csproj`
- **PluginLoader:** `tools/DdsMonitor/DdsMonitor.Engine/Plugins/PluginLoader.cs`
- **Core tests:** `tests/DdsMonitor.Avalonia.Core.Tests/`
- **Feature demo tests:** `tests/DdsMonitor.Avalonia.FeatureDemoPlugin.Tests/` *(create if missing)*

### Build & Test Commands

```powershell
# Build the whole solution
dotnet build CycloneDDS.NET.sln -c Debug

# Run only the affected test projects
dotnet test tests/DdsMonitor.Avalonia.Core.Tests/ -c Debug
dotnet test tests/DdsMonitor.Avalonia.FeatureDemoPlugin.Tests/ -c Debug

# Verify the StagePlugin MSBuild target placed the DLL
Test-Path "tools/DdsMonitor/DdsMonitor.Avalonia/bin/Debug/net8.0/plugins/DdsMonitor.Avalonia.FeatureDemoPlugin.dll"
```

### Report Submission

When done, submit your report to:
`.dev-workstream/avalonia-2/reports/BATCH-01-REPORT.md`

If you have questions, create:
`.dev-workstream/avalonia-2/questions/BATCH-01-QUESTIONS.md`

---

## Context

This batch creates the two layers everything else builds on:

1. **M1-T0** (Preflight): Adds Dock.NET package references to the shell csproj,
   creates the `DdsMonitor.Avalonia.FeatureDemoPlugin` project skeleton, registers
   it in the solution, and adds/verifies test project skeletons.

2. **M1-T1** (Core Abstractions): Adds `LayoutKind`, updates `ToolbarEntry` with a
   `Label` field, adds all six service interfaces (`IContextMenuPresenter`,
   `IFileDialogService`, `IKeyboardShortcutService`, `IThemeService`,
   `IClipboardService`, `IUiThreadInvoker`) plus the `IUiThreadInvoker`
   production implementation, and updates `IEventBrokerExtensions`.

All subsequent batches depend on these two tasks being complete and correct.

---

## 🎯 Batch Objectives

- Dock.NET packages present in the shell csproj (both `Dock.Avalonia` and
  `Dock.Model.Mvvm`, version 11.2.x compatible with Avalonia 11.2.3).
- `DdsMonitor.Avalonia.FeatureDemoPlugin` project compiles and its DLL is
  staged into the shell's `plugins/` folder on build.
- All six new service interfaces exist in `DdsMonitor.Avalonia.Core` with the
  exact signatures from TASK-DETAILS.md §M1-T1.
- `LayoutKind` enum exists.
- `ToolbarEntry` has a `Label` field (field order: `Id`, `Action`, `Label`,
  `Tooltip`, `IconKey?`).
- All Core unit tests pass (≥ 1 new test per interface/type added).
- `DdsMonitor.Avalonia.Core` must not reference Dock.NET or the shell project.

---

## ✅ Tasks

### Task 1: M1-T0 — Preflight

**Full spec:** See [TASK-DETAILS.md §M1-T0](../../TASK-DETAILS.md#m1-t0--preflight-solution-skeleton-and-shared-assemblies)

**Summary of what to do:**

1. Add to `DdsMonitor.Avalonia.csproj`:
   - `<PackageReference Include="Dock.Avalonia" Version="11.2.0" />`
   - `<PackageReference Include="Dock.Model.Mvvm" Version="11.2.0" />`
   - A `ProjectReference` to the new FeatureDemoPlugin csproj with
     `ReferenceOutputAssembly="false" Private="false" SkipGetTargetFrameworkProperties="true"`.

2. Verify `DdsMonitor.Engine/Plugins/PluginLoader.cs` already contains both
   `"DdsMonitor.Engine"` and `"DdsMonitor.Avalonia.Core"` in `SharedAssemblyNames`.
   They ARE already present — no change needed. Just confirm in your report.

3. Create `tools/DdsMonitor/DdsMonitor.Avalonia.FeatureDemoPlugin/`:
   - `DdsMonitor.Avalonia.FeatureDemoPlugin.csproj` — mirror StandardPlugin csproj
     (same TargetFramework, Nullable, ImplicitUsings, LangVersion, StagePlugin target).
     Reference `DdsMonitor.Engine`, `DdsMonitor.Avalonia.Core`, `Avalonia 11.2.3`,
     `Microsoft.Extensions.Hosting.Abstractions 8.0.0`.
     Add `InternalsVisibleTo` for `DdsMonitor.Avalonia.FeatureDemoPlugin.Tests`.
   - `Placeholder.cs` — `internal static class Placeholder { }` (will be deleted in M1-T10).

4. Add `DdsMonitor.Avalonia.FeatureDemoPlugin` to `CycloneDDS.NET.sln` under the
   `DdsMonitor` solution folder, with Debug/Release Any CPU configs.

5. Create (if missing) test project skeletons:
   - `tests/DdsMonitor.Avalonia.FeatureDemoPlugin.Tests/DdsMonitor.Avalonia.FeatureDemoPlugin.Tests.csproj`
     (xUnit 2.9.x, Microsoft.NET.Test.Sdk 17.x, references the FeatureDemoPlugin project).
   - Placeholder test class: `public class PlaceholderTests { [Fact] public void True_is_true() => Assert.True(true); }`

   Check if these already exist and only create what's missing:
   - `tests/DdsMonitor.Avalonia.Tests/` — **already exists**
   - `tests/DdsMonitor.Avalonia.Core.Tests/` — **already exists**
   - `tests/DdsMonitor.Avalonia.StandardPlugin.Tests/` — check if exists
   - `tests/DdsMonitor.Avalonia.FeatureDemoPlugin.Tests/` — likely missing, create it

6. Register any newly created test projects in `CycloneDDS.NET.sln`.

**⚠️ IMPORTANT about existing tests:** The existing `DdsMonitor.Avalonia.Core.Tests`
already has real tests (not placeholders). Do not overwrite them. Only add the
FeatureDemoPlugin test project if it doesn't exist.

**Acceptance criteria (from TASK-DETAILS.md):**
- `dotnet build CycloneDDS.NET.sln -c Debug` succeeds with zero errors.
- After Debug build, `tools/DdsMonitor/DdsMonitor.Avalonia/bin/Debug/net8.0/plugins/DdsMonitor.Avalonia.FeatureDemoPlugin.dll` exists.
- `Dock.Avalonia` and `Dock.Model.Mvvm` package references appear in the shell csproj.
- `DdsMonitor.Avalonia.Core` is confirmed present in `PluginLoader.SharedAssemblyNames`.
- The solution file lists the new plugin and the FeatureDemoPlugin test project.

---

### Task 2: M1-T1 — Avalonia.Core Abstractions

**Full spec:** See [TASK-DETAILS.md §M1-T1](../../TASK-DETAILS.md#m1-t1--avaloniacore-abstractions)

**Summary of what to do:**

All new files go under `tools/DdsMonitor/DdsMonitor.Avalonia.Core/`.

1. **`LayoutKind.cs`** — enum: `Mdi`, `DockDocument`, `DockTool`.

2. **Modify `ToolbarEntry.cs`** — add `Label` field (short visible button text).
   New positional order: `Id`, `Action`, `Label`, `Tooltip`, `IconKey?`.
   Update the `IToolbarRegistry.Register` signature to match:
   `Register(string id, Action onClick, string label = "", string tooltip = "", string? iconKey = null)`.
   Also update `ToolbarRegistry.cs` implementation.
   **Search for all call sites** in StandardPlugin (`*Plugin.cs` files) and fix them.

3. **`IContextMenuPresenter.cs`** — interface with:
   ```csharp
   void Show(Control anchor, object dataContext,
             IReadOnlyList<DdsMonitor.Engine.Plugins.ContextMenuItem>? defaultItems = null);
   ```
   *Interface only here — implementation is M1-T8.*

4. **`IFileDialogService.cs`** — interface with:
   ```csharp
   Task<string?> OpenFileAsync(string title, IReadOnlyList<FilePickerFilter> filters,
                               string? initialDirectory = null);
   Task<string?> SaveFileAsync(string title, string suggestedName,
                               IReadOnlyList<FilePickerFilter> filters);
   ```
   Co-locate `record FilePickerFilter(string Name, IReadOnlyList<string> Extensions)`.

5. **`IKeyboardShortcutService.cs`** — interface with:
   ```csharp
   void Register(KeyGesture gesture, string description, Action action);
   IReadOnlyList<RegisteredShortcut> Registered { get; }
   bool TryInvoke(KeyGesture gesture);
   ```
   Co-locate `record RegisteredShortcut(KeyGesture Gesture, string Description, Action Action)`.

6. **`IThemeService.cs`** — enum `ThemeMode { System, Light, Dark }` and interface:
   ```csharp
   ThemeMode CurrentMode { get; }
   event Action<ThemeMode>? ModeChanged;
   void SetMode(ThemeMode mode);
   ```
   Note: implementation is M1-T8; this task only defines the interface + enum.

7. **`IClipboardService.cs`** — interface:
   ```csharp
   Task SetTextAsync(string text);
   Task<string?> GetTextAsync();
   ```

8. **`IUiThreadInvoker.cs`** — interface:
   ```csharp
   bool CheckAccess();
   void Post(Action action);
   Task InvokeAsync(Func<Task> action);
   ```
   Plus `AvaloniaUiThreadInvoker` (production implementation, same file or
   co-located) wrapping `Dispatcher.UIThread`.

9. **Modify `IEventBrokerExtensions.cs`** — add an overload:
   ```csharp
   SubscribeOnUiThread<TEvent>(this IEventBroker broker, Action<TEvent> handler,
                               IUiThreadInvoker invoker)
   ```
   Keep the existing `Dispatcher?` overload.

10. **Unit tests** in `tests/DdsMonitor.Avalonia.Core.Tests/`:

    **`ToolbarRegistryTests.cs`** (new file — the existing test file is
    `AvaloniaCoreSuite.cs` which tests UserSettingsStore; do not delete it):
    - `Register_SameId_Replaces`: registering with duplicate id replaces entry.
    - `Register_FiresChanged`: `ToolbarRegistry.Changed` event fires on registration.
    - `Entries_IsSnapshot`: `Entries` returns a snapshot (modifying it doesn't
      affect the registry).

    **`IUiThreadInvokerTests.cs`**:
    - Create a `SynchronousInvoker` (test helper that runs everything inline).
    - `Post_ExecutesAction`: posted action executes.
    - `InvokeAsync_ExecutesFunc`: async func executes and task completes.

    **`AvaloniaViewRegistryTests.cs`**:
    - `Register_ThenBuildView_ReturnsControl`: register VM type → view type mapping,
      build returns the expected control type.
    - `Build_UnregisteredType_ThrowsInvalidOperationException`.

    **Note on existing tests:** `AvaloniaCoreSuite.cs` has `UserSettingsStoreTests`.
    Do not duplicate. The task spec also asks for `UserSettingsStoreTests.cs` but
    those already exist — just verify they still pass.

**⚠️ CRITICAL:** `DdsMonitor.Avalonia.Core` must NOT reference Dock.NET or the
shell project. All interfaces use Avalonia types that are already referenced.

**Acceptance criteria (from TASK-DETAILS.md):**
- `dotnet build` succeeds with zero errors.
- `dotnet test tests/DdsMonitor.Avalonia.Core.Tests/` passes; all four new test
  classes report ≥ 1 passing test each.
- All six interfaces + `LayoutKind` + `IUiThreadInvoker` exist with the correct signatures.
- `ToolbarEntry` has a `Label` field; all existing call sites compile.
- No file in `Avalonia.Core/` references Dock.NET, the shell project, or any plugin.

---

## 🧪 Testing Requirements

**Minimum tests:**
- `ToolbarRegistryTests.cs`: 3 tests (replace, changed event, snapshot).
- `IUiThreadInvokerTests.cs`: 2 tests (Post, InvokeAsync).
- `AvaloniaViewRegistryTests.cs`: 2 tests (happy path, throws).

**Quality standards:**
- Each test must have a clear Arrange/Act/Assert structure.
- Tests must assert *behavior*, not just "it didn't throw".
- No `Assert.True(true)` placeholder tests.
- Tests must be deterministic (no random delays).

---

## 📊 Developer Insights Required in Report

Answer these questions in your report:

1. **Dock.NET version**: What exact version of `Dock.Avalonia` and `Dock.Model.Mvvm`
   was found and used? Was 11.2.0 available or did you use a different 11.2.x patch?

2. **ToolbarEntry call sites**: List every file where `IToolbarRegistry.Register(...)`
   was found and updated. Were there any call sites you chose NOT to update (and why)?

3. **Existing tests**: What tests were already present in `DdsMonitor.Avalonia.Core.Tests`?
   Did any fail after your changes? If so, how did you fix them?

4. **Interface signatures**: Were there any conflicts between existing code and the
   new interfaces you were asked to add? How were they resolved?

5. **Weak points spotted**: What potential issues did you notice in the existing codebase
   that are not part of this batch but may cause problems later?

6. **Design decisions made beyond spec**: Did you make any implementation choices
   not explicitly stated in TASK-DETAILS.md? Document them here.

---

## 📋 Report Format

Submit your report to `.dev-workstream/avalonia-2/reports/BATCH-01-REPORT.md`.

Structure:
```markdown
# BATCH-01 Report

## Tasks Completed
- M1-T0: [status]
- M1-T1: [status]

## Build & Test Results
[Full output of: dotnet build CycloneDDS.NET.sln -c Debug]
[Full output of: dotnet test tests/DdsMonitor.Avalonia.Core.Tests/]
[Full output of: dotnet test tests/DdsMonitor.Avalonia.FeatureDemoPlugin.Tests/]
[Result of StagePlugin verification]

## Developer Insights
[Answer all 6 questions above]

## Files Changed
[List every file created, modified, or deleted]

## Known Issues
[Any known issues or deferred items]
```

---

## 🔄 Mandatory Workflow: Test-Driven Task Progression

You MUST follow this sequence for each task:

1. **Read the full task spec** from TASK-DETAILS.md before writing any code.
2. **Write the test(s) first** (where practical), confirm they fail.
3. **Implement** the production code to make the tests pass.
4. **Run `dotnet build`** — zero errors required before moving on.
5. **Run `dotnet test`** for the affected projects — all tests must be green.
6. **Only then** move to the next task.

**Do not skip tests.** A task is not done until its tests pass. Report any test
that you couldn't make pass with a clear explanation.
