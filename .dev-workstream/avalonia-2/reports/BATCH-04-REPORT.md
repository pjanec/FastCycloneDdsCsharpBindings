# BATCH-04 Completion Report

**Branch:** `ddsmon-avalonia`  
**Tasks:** Corrective T0 (C0-1, C0-2, C0-3) + M1-T7 + M1-T8 + M1-T9  
**Result:** ✅ COMPLETE

---

## Validation Results

```
dotnet build CycloneDDS.NET.sln -c Debug       → 0 errors, 0 warnings (blocking)
dotnet test tests/DdsMonitor.Avalonia.Tests/    → Passed: 95, Failed: 0
dotnet test tests/DdsMonitor.Avalonia.Core.Tests/ → Passed: 27, Failed: 0
```

---

## Files Changed / Created

### New production files
- `tools/DdsMonitor/DdsMonitor.Avalonia/Services/ContextMenuPresenter.cs`
- `tools/DdsMonitor/DdsMonitor.Avalonia/Services/FileDialogService.cs`
- `tools/DdsMonitor/DdsMonitor.Avalonia/Services/KeyboardShortcutService.cs`
- `tools/DdsMonitor/DdsMonitor.Avalonia/Services/ThemeService.cs`
- `tools/DdsMonitor/DdsMonitor.Avalonia/Services/ClipboardService.cs`

### Modified production files
- `tools/DdsMonitor/DdsMonitor.Avalonia/ShellWindow.axaml` — full replacement with DockControl layout
- `tools/DdsMonitor/DdsMonitor.Avalonia/ShellWindow.axaml.cs` — full replacement with IServiceProvider constructor
- `tools/DdsMonitor/DdsMonitor.Avalonia/App.axaml.cs` — updated ShellWindow instantiation
- `tools/DdsMonitor/DdsMonitor.Avalonia/Program.cs` — fixed double-call bug + 7 new DI registrations
- `tools/DdsMonitor/DdsMonitor.Avalonia.Core/AvaloniaTypeDrawerRegistry.cs` — Bug §13.3 fixed
- `tools/DdsMonitor/DdsMonitor.Avalonia.StandardPlugin/TopicExplorerViewModel.cs` — Bug §13.4 fixed
- `tools/DdsMonitor/DdsMonitor.Avalonia.StandardPlugin/TopicExplorerPlugin.cs` — Bug §13.6 fixed

### New test files
- `tests/DdsMonitor.Avalonia.Tests/ShellWindowTests.cs` — `ShellWindowNewTests` class (4 tests)
- `tests/DdsMonitor.Avalonia.Tests/ProgramRegistrationTests.cs` — DI resolution test (1 test)
- `tests/DdsMonitor.Avalonia.Tests/Services/KeyboardShortcutServiceTests.cs` — 4 tests
- `tests/DdsMonitor.Avalonia.Tests/Services/ThemeServiceTests.cs` — 3 tests
- `tests/DdsMonitor.Avalonia.Tests/Services/ContextMenuPresenterTests.cs` — 2 tests

### Modified test files
- `tests/DdsMonitor.Avalonia.Tests/AvaloniaWindowManagerTests.cs` — added C0-1, C0-2, C0-3 + `RecordingEventBroker`
- `tests/DdsMonitor.Avalonia.Tests/ShellTests.cs` — updated for new ShellWindow API
- `tests/DdsMonitor.Avalonia.StandardPlugin.Tests/StandardPluginSuite.cs` — renamed auto-spawn test

---

## Developer Insight Questions

### 1. C0 tests — Were all 3 missing tests straightforward?

Yes. The 3 tests were natural extensions of the existing `AvaloniaWindowManagerDockTests` class. The only infrastructure needed was `RecordingEventBroker` — a private nested class that stores published events in a `List<object>`. This was trivial: one field, one `Publish<T>` override appending the boxed event. The trickiest part of C0-2 was identifying the correct event type (`WorkspaceSaveRequestedEvent`) from the Avalonia `AvaloniaWindowManager.ClosePanel` method. C0-3 (round-trip JSON) was clean — call `SaveWorkspaceToJson`, assert the JSON string contains the expected literal tokens.

### 2. ShellWindow + DockControl — How was DockControl.Layout wired?

`DockControl` has a `Layout` dependency property of type `IDock`. The `IDockManager` implementation (`DockManager`) acts as the factory for `DockLayout` objects and owns the dock factory. In `ShellWindow.axaml.cs`, after `InitializeComponent()`:

```csharp
var dockMgr = services.GetService<IDockManager>();
dockMgr?.Initialise(MainDock);   // sets MainDock.Layout = dockMgr.CreateLayout()
```

`IDockManager.Initialise(DockControl)` assigns the layout at the right time — after the visual tree is constructed but before the window is shown. No order-of-initialization issue arose because `IDockManager` is resolved lazily from the DI container; the `DockControl` already exists at that point since `InitializeComponent()` was called first.

### 3. ThemeService — Does setting RequestedThemeVariant actually switch the FluentTheme?

`Application.RequestedThemeVariant` is Avalonia's first-class theme switch mechanism. When `App.axaml` declares `<FluentTheme />` without an explicit `Mode`, it defaults to respecting `Application.RequestedThemeVariant`. Setting it to `ThemeVariant.Dark` at runtime causes Avalonia's styling engine to re-evaluate the entire theme resource tree via `ResourceDictionary` merging. The visual change is immediate on the UI thread.

**Important caveat**: `RequestedThemeVariant` setter must be called on the UI thread. This was the cause of a test failure: when `ThemeService` is constructed from a non-UI-thread test context (even with a headless Avalonia app active), the setter throws. The fix was to add `if (!Dispatcher.UIThread.CheckAccess()) return;` in `Apply()`. The `ThemeServiceTests` tests use `[AvaloniaFact]` so they always execute on the UI thread — those tests verify the real theme switch. `ProgramRegistrationTests` (a plain `[Fact]`) only verifies DI resolution, not theme application.

### 4. Program.cs lazy providers — Func<TopLevel?> implementation

`FileDialogService` and `ClipboardService` both need a `TopLevel` to call `StorageProvider` / `Clipboard`. The main window isn't yet created at DI registration time, so a lambda is captured that looks it up at first use:

```csharp
// FileDialogService — needs Visual (parent for dialog)
services.AddSingleton<IFileDialogService>(_ =>
    new FileDialogService(() =>
        Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lt
        ? lt.MainWindow as Visual : null));

// ClipboardService — needs TopLevel
services.AddSingleton<IClipboardService>(_ =>
    new ClipboardService(() =>
        Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime lt2
        ? TopLevel.GetTopLevel(lt2.MainWindow) : null));
```

The lambda is not invoked during `BuildServiceProvider()`. It is only called when the first `OpenFileAsync()` / `SetTextAsync()` call reaches the service — by which time `desktop.MainWindow` has been set by `ShellWindow` startup. This avoids any null-dereference during the DI resolution phase.

### 5. TopicExplorerPlugin auto-spawn removal — Did it break existing tests?

Yes, it broke exactly one existing test: `TopicExplorerPlugin_Initialize_SpawnsTopicExplorerPanel` in `StandardPluginSuite.cs`, which asserted `Assert.Contains(win.SpawnCalls, ...)`. This test was renamed to `TopicExplorerPlugin_Initialize_DoesNotAutoSpawnPanel` and inverted to `Assert.DoesNotContain`. No other tests depended on the auto-spawn behavior. The menu item and toolbar button that allow *manual* spawning were kept — only the unconditional startup spawn was removed.

### 6. Weak points — Tests with headless constraints?

Two test areas are behaviorally weaker due to headless limitations:

- **`ThemeServiceTests`**: The tests verify `RequestedThemeVariant` is set on `Application.Current`, but the headless test app's `FluentTheme` doesn't actually render. So we confirm the property value changed, but cannot verify that controls visually switch from light to dark. This is acceptable — visual appearance is an integration concern.

- **`ContextMenuPresenterTests`**: `Show()` calls `ContextMenu.Open(anchor)` which in a headless environment opens a popup with no real display. The test asserts `menu.Items.Count == 3` by constructing the menu and checking its items, but doesn't verify that the popup actually appeared at the correct screen coordinates. This behavioral gap is inherent to headless testing and documented.

- **`ProgramRegistrationTests`**: `ThemeService` construction skips the `Apply()` call on non-UI threads (see Q3), so the test only validates DI wiring, not theme application. This is intentional: DI resolution tests should not require a UI thread.
