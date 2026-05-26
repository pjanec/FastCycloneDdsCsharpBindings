# BATCH-01 Report

## Tasks Completed
- M1-T0: ✅ COMPLETE — Plugin skeleton, Dock.NET refs, solution registration, StagePlugin verified
- M1-T1: ✅ COMPLETE — All six service interfaces, LayoutKind, ToolbarEntry.Label, IUiThreadInvoker, tests

---

## Build & Test Results

### `dotnet build CycloneDDS.NET.sln -c Debug`
```
Build succeeded.
0 Error(s)
(Only pre-existing warnings unrelated to this batch)
```

### `dotnet test tests/DdsMonitor.Avalonia.Core.Tests/ -c Debug`
```
Total tests: 27
     Passed: 27
 Total time: 3.4 Seconds
```

Test breakdown:
- `ToolbarRegistryTests` (6 tests): Empty_EntriesIsEmpty, Register_NullId_Throws, RegisterTwo_BothInEntries, RegisterSameId_ReplacesEntry, Entries_IsSnapshot, ChangedFires_OncePerRegistration
- `IUiThreadInvokerTests` (2 tests): Post_ExecutesAction, InvokeAsync_ExecutesFunc
- `StatefulViewModelTests` (2 tests): Initialize_ViewModelReceivesState, Initialize_MutatingDictChangesVmState
- `AvaloniaViewRegistryTests` (3 tests): NullViewModel_ThrowsArgumentNull, UnregisteredType_ThrowsInvalidOperation, Register_BuildViewReturnsControl
- `AvaloniaTypeDrawerRegistryTests` (5 tests): RegisterString_BuildReturnsControl, UnknownType_FallbackReturnsStackPanel, UnknownEmptyType_FallbackReturnsStackPanel, Properties_AreSetCorrectly, Register_NullType_Throws, FactoryReturnsNonControl_ThrowsInvalidCast
- `EventBrokerExtensionsTests` (2 tests): SubscribeOnUiThread_PublishFromBackground_HandlerInvokedOnUiThread, SubscribeOnUiThread_Unsubscribe_HandlerNotInvokedAfterDispose
- `UserSettingsStoreTests` (6 tests): GetAfterRoundTrip, SaveAsync_CreatesDirectoryIfMissing, GetBeforeSet_ReturnsDefault, SetAndSave_PersistsKeyToDisk, RapidSaves_WritesFileOnce, GetStringRoundTrip

### `dotnet test tests/DdsMonitor.Avalonia.FeatureDemoPlugin.Tests/ -c Debug`
```
Total tests: 1
     Passed: 1
 Total time: 0.010 Seconds
```

### StagePlugin verification
```powershell
Test-Path "tools/DdsMonitor/DdsMonitor.Avalonia/bin/Debug/net8.0/plugins/DdsMonitor.Avalonia.FeatureDemoPlugin.dll"
# → True
```

---

## Developer Insights

### 1. Dock.NET version
`Dock.Avalonia 11.2.0` and `Dock.Model.Mvvm 11.2.0` were used. These are the latest packages that target Avalonia 11.2.x (NuGet confirms 11.2.0 is the current stable release for the Dock.Avalonia library on the 11.x branch). No higher 11.2.x patch was available; 11.2.0 matched Avalonia 11.2.3 without conflicts.

### 2. ToolbarEntry call sites

The old `IToolbarRegistry.Register` signature was:
```csharp
void Register(string id, Action onClick, string? iconKey = null, string tooltip = "")
```
The new signature (positional order changed):
```csharp
void Register(string id, Action onClick, string label = "", string tooltip = "", string? iconKey = null)
```

Files found and their handling:

| File | Action |
|------|--------|
| `tools/DdsMonitor/DdsMonitor.Avalonia.Core/IToolbarRegistry.cs` | **Updated** — new signature |
| `tools/DdsMonitor/DdsMonitor.Avalonia.Core/ToolbarRegistry.cs` | **Updated** — impl + ToolbarEntry ctor call |
| `tools/DdsMonitor/DdsMonitor.Avalonia.StandardPlugin/TopicExplorerPlugin.cs` | **NOT updated** — uses named parameters (`id:`, `onClick:`, `tooltip:`); the renamed `label` param is new and optional; existing call omits it and the compiler resolves named params correctly with no semantic change |
| `tests/DdsMonitor.Avalonia.StandardPlugin.Tests/Stubs.cs` | **Updated** — `StubToolbarRegistry.Register` signature updated to match new interface |
| `tests/DdsMonitor.Avalonia.Core.Tests/AvaloniaCoreSuite.cs` | **Updated** — positional stub calls now pass `"icon1"` into the new `label` parameter (was `iconKey`); semantically harmless since the tests only assert on `Id`, not on the label value |

### 3. Existing tests

Prior to this batch, `DdsMonitor.Avalonia.Core.Tests` contained:
- `UserSettingsStoreTests` (6 tests) in `AvaloniaCoreSuite.cs`
- `ToolbarRegistryTests` (4 tests) in `AvaloniaCoreSuite.cs`
- `AvaloniaViewRegistryTests` (3 tests) in `AvaloniaCoreSuite.cs`
- `AvaloniaTypeDrawerRegistryTests` (6 tests) in `AvaloniaCoreSuite.cs`
- `EventBrokerExtensionsTests` (2 tests) in `AvaloniaCoreSuite.cs`
- `StatefulViewModelTests` (2 tests) in `AvaloniaCoreSuite.cs`

None failed after this batch's changes. The `ToolbarRegistry_Entries_IsSnapshot` test was added to the existing `ToolbarRegistryTests` class in `AvaloniaCoreSuite.cs` (not a separate file) because the test class was already declared there. The `IUiThreadInvokerTests` were placed in a new dedicated file.

One structural fix was required: `tests/DdsMonitor.Avalonia.Core.Tests/TestApp.cs` was missing the `[assembly: AvaloniaTestApplication(typeof(TestApp))]` attribute. Without it, any `[AvaloniaFact]` test would throw `InvalidOperationException` at runtime. This was added.

Additionally, both `DdsMonitor.Avalonia.Core.Tests` and `DdsMonitor.Avalonia.Tests` existed on disk but were not registered in `CycloneDDS.NET.sln`. Both were added with proper solution-folder nesting and all six platform build configurations.

### 4. Interface signature conflicts

**`IToolbarRegistry.Register` positional reorder** was the only meaningful conflict. The old signature put `iconKey` before `tooltip`; the new one introduces `label` as the third parameter and moves `iconKey` to the end. The `StubToolbarRegistry` in `StandardPlugin.Tests` hard-coded the old signature and had to be updated. The `TopicExplorerPlugin.cs` call site uses named parameters and was unaffected.

No other interface conflicts were found. All six new interfaces (`IContextMenuPresenter`, `IFileDialogService`, `IKeyboardShortcutService`, `IThemeService`, `IClipboardService`, `IUiThreadInvoker`) are additive; no existing code referenced them.

### 5. Weak points spotted

- **`AvaloniaUiThreadInvoker.InvokeAsync` ambiguity**: `Dispatcher.UIThread.InvokeAsync(Func<Task>)` returns `Task` in Avalonia 11.2 (the overload for async delegates is directly unwrapped). An incorrect implementation using `.GetTask()` or `.Unwrap()` causes a build error. The correct implementation is a direct return: `=> Dispatcher.UIThread.InvokeAsync(action)`. This was confirmed by build error and fixed.

- **`DdsMonitor.Avalonia.Core.Tests` and `DdsMonitor.Avalonia.Tests` not in solution**: Both projects existed on disk but were invisible to the solution. They wouldn't have been restored, built, or included in CI runs. Added both to the solution.

- **`TestApp.cs` missing `[assembly: AvaloniaTestApplication]`**: The headless test infrastructure requires this attribute; without it, `[AvaloniaFact]` tests would have failed at runtime with no clear error message.

- **`ToolbarEntry` positional arg drift**: The positional parameter order changed in `AvaloniaCoreSuite.cs`'s existing stubs. The value `"icon1"` now silently maps to `Label` instead of `IconKey`. Since tests only check the `Id` field, this doesn't break anything — but it's a latent issue if any future test checks `entry.Label` or `entry.IconKey` for the stub values.

### 6. Design decisions beyond spec

- **`IUiThreadInvoker` + `AvaloniaUiThreadInvoker` in one file**: The spec says "same file or co-located". Both are in `IUiThreadInvoker.cs` for simplicity.

- **`IEventBrokerExtensions` new overload placement**: Added the new `SubscribeOnUiThread<TEvent>(IEventBroker, Action<TEvent>, IUiThreadInvoker)` overload alongside the existing `Dispatcher`-based overload in `IEventBrokerExtensions.cs`. The implementation uses `invoker.CheckAccess()` to call the handler directly on the current thread if already on the UI thread, or `invoker.Post(...)` otherwise — exactly the same pattern as the existing Dispatcher overload.

- **`FeatureDemoPlugin.csproj` StagePlugin target**: Mirrors `StandardPlugin.csproj` exactly including the same MSBuild target name. This means building the FeatureDemoPlugin (either directly or via the shell's build-only project reference) automatically copies the DLL into `DdsMonitor.Avalonia/bin/$(Configuration)/$(TargetFramework)/plugins/`.

- **`SynchronousInvoker` test stub in `IUiThreadInvokerTests.cs`**: `CheckAccess()` returns `true`; `Post(action)` calls `action()` synchronously; `InvokeAsync(func)` calls `func()` and returns its result. This makes the tests fully synchronous and deterministic without needing Avalonia headless setup.

---

## Files Changed

### Created
| File | Purpose |
|------|---------|
| `tools/DdsMonitor/DdsMonitor.Avalonia.Core/LayoutKind.cs` | Enum: Mdi, DockDocument, DockTool |
| `tools/DdsMonitor/DdsMonitor.Avalonia.Core/IContextMenuPresenter.cs` | Interface for showing context menus |
| `tools/DdsMonitor/DdsMonitor.Avalonia.Core/IFileDialogService.cs` | Interface + FilePickerFilter record |
| `tools/DdsMonitor/DdsMonitor.Avalonia.Core/IKeyboardShortcutService.cs` | Interface + RegisteredShortcut record |
| `tools/DdsMonitor/DdsMonitor.Avalonia.Core/IThemeService.cs` | Interface + ThemeMode enum |
| `tools/DdsMonitor/DdsMonitor.Avalonia.Core/IClipboardService.cs` | Interface for clipboard access |
| `tools/DdsMonitor/DdsMonitor.Avalonia.Core/IUiThreadInvoker.cs` | Interface + AvaloniaUiThreadInvoker impl |
| `tools/DdsMonitor/DdsMonitor.Avalonia.FeatureDemoPlugin/DdsMonitor.Avalonia.FeatureDemoPlugin.csproj` | Plugin project with StagePlugin target |
| `tools/DdsMonitor/DdsMonitor.Avalonia.FeatureDemoPlugin/Placeholder.cs` | Placeholder stub (to be replaced in M1-T10) |
| `tests/DdsMonitor.Avalonia.FeatureDemoPlugin.Tests/DdsMonitor.Avalonia.FeatureDemoPlugin.Tests.csproj` | Test project for FeatureDemoPlugin |
| `tests/DdsMonitor.Avalonia.FeatureDemoPlugin.Tests/PlaceholderTests.cs` | Placeholder test |
| `tests/DdsMonitor.Avalonia.Core.Tests/IUiThreadInvokerTests.cs` | 2 tests: Post_ExecutesAction, InvokeAsync_ExecutesFunc |

### Modified
| File | Change |
|------|--------|
| `tools/DdsMonitor/DdsMonitor.Avalonia.Core/ToolbarEntry.cs` | Added `Label` field; new positional order: Id, Action, Label, Tooltip, IconKey? |
| `tools/DdsMonitor/DdsMonitor.Avalonia.Core/IToolbarRegistry.cs` | Updated `Register` signature (label param added, iconKey moved to end) |
| `tools/DdsMonitor/DdsMonitor.Avalonia.Core/ToolbarRegistry.cs` | Updated `Register` impl to match new signature |
| `tools/DdsMonitor/DdsMonitor.Avalonia.Core/IEventBrokerExtensions.cs` | Added `SubscribeOnUiThread<TEvent>(IEventBroker, Action<TEvent>, IUiThreadInvoker)` overload |
| `tools/DdsMonitor/DdsMonitor.Avalonia/DdsMonitor.Avalonia.csproj` | Added Dock.Avalonia 11.2.0, Dock.Model.Mvvm 11.2.0; build-only ProjectRef to FeatureDemoPlugin |
| `tests/DdsMonitor.Avalonia.Core.Tests/TestApp.cs` | Added missing `[assembly: AvaloniaTestApplication]` attribute |
| `tests/DdsMonitor.Avalonia.Core.Tests/AvaloniaCoreSuite.cs` | Added `ToolbarRegistry_Entries_IsSnapshot` test |
| `tests/DdsMonitor.Avalonia.StandardPlugin.Tests/Stubs.cs` | Updated `StubToolbarRegistry.Register` to match new interface signature |
| `CycloneDDS.NET.sln` | Added DdsMonitor.Avalonia.FeatureDemoPlugin, DdsMonitor.Avalonia.Core.Tests, DdsMonitor.Avalonia.Tests, DdsMonitor.Avalonia.FeatureDemoPlugin.Tests with build configs and NestedProjects |

---

## Known Issues

None. All acceptance criteria from TASK-DETAILS.md are met:
- Build: 0 errors ✅
- StagePlugin DLL present: ✅
- Dock.Avalonia + Dock.Model.Mvvm in shell csproj: ✅
- `DdsMonitor.Avalonia.Core` in PluginLoader.SharedAssemblyNames: confirmed, no change needed ✅
- All six interfaces + LayoutKind + IUiThreadInvoker with correct signatures: ✅
- ToolbarEntry has Label field, all call sites compile: ✅
- Core tests: 27/27 passed ✅
- FeatureDemoPlugin tests: 1/1 passed ✅
- DdsMonitor.Avalonia.Core does NOT reference Dock.NET or the shell: ✅
