# BATCH-04 Instructions

**Branch:** `ddsmon-avalonia`  
**Tasks:** Corrective T0 (3 missing WindowManager tests) + M1-T7 (ShellWindow) + M1-T8 (Service implementations) + M1-T9 (Program.cs DI + bug fixes)  
**Priority order:** Complete Corrective T0 first; then M1-T7; then M1-T8; then M1-T9 (they are strictly sequential — each depends on the previous).

---

## Prerequisites — Read First

1. Read `.github/skills/developer/SKILL.md`.
2. Read `.dev-workstream/avalonia-2/DESIGN.md` **end-to-end** (especially §3, §7, §8, §9, §13).
3. Read `.dev-workstream/avalonia-2/TASK-DETAILS.md` sections for M1-T7, M1-T8, M1-T9 in full.
4. Read `.dev-workstream/avalonia-2/reviews/BATCH-03-REVIEW.md` for the corrective items.
5. Read the existing `ShellWindow.axaml` and `ShellWindow.axaml.cs` before replacing.
6. Read the existing `Program.cs` before modifying.

---

## Corrective Task 0 — Add Missing AvaloniaWindowManager Tests (from BATCH-03 review)

**File:** `tests/DdsMonitor.Avalonia.Tests/AvaloniaWindowManagerTests.cs`

Add 3 new `[AvaloniaFact]` tests to the existing `AvaloniaWindowManagerDockTests` class.

### C0-1: `LoadWorkspaceFromJson_BlazorFormat_AllMdi`

Blazor workspace JSON format (no `LayoutKind`, no `DockLayout`) must produce MDI panels only.

```csharp
[AvaloniaFact]
public void LoadWorkspaceFromJson_BlazorFormat_AllMdi()
{
    var (manager, mdiHost) = CreateManager();
    var window = new Window { Content = mdiHost, Width = 800, Height = 600 };
    window.Show();
    try
    {
        // Blazor-format JSON — no LayoutKind, no DockLayout
        var json = """
            {
                "Panels": [
                    { "PanelId": "p1", "Title": "P1", "ComponentTypeName": "Unknown1", "ComponentState": {} },
                    { "PanelId": "p2", "Title": "P2", "ComponentTypeName": "Unknown2", "ComponentState": {} }
                ],
                "ExcludedTopics": []
            }
            """;

        manager.LoadWorkspaceFromJson(json);

        // Both panels should be spawned as MDI (default)
        Assert.Equal(2, manager.ActivePanels.Count);
        Assert.Equal(2, mdiHost.Children.Count);
    }
    finally { window.Close(); }
}
```

### C0-2: `ClosePanel_PublishesWorkspaceSaveRequestedEvent`

```csharp
[AvaloniaFact]
public void ClosePanel_PublishesWorkspaceSaveRequestedEvent()
{
    // Use a StubEventBroker that records published events
    var published = new List<object>();
    var broker = new RecordingEventBroker(published);
    var mdiHost = new MdiHost { Width = 800, Height = 600 };
    var manager = new AvaloniaWindowManager(new StubViewRegistry(), new StubServiceProvider(), broker);
    manager.SetMdiHost(mdiHost);

    var window = new Window { Content = mdiHost, Width = 800, Height = 600 };
    window.Show();
    try
    {
        manager.SpawnPanel("Panel1");
        Assert.Equal(1, manager.ActivePanels.Count);

        manager.ClosePanel("Panel1");

        Assert.Contains(published, e => e is WorkspaceSaveRequestedEvent);
    }
    finally { window.Close(); }
}
```

You will need to add a `RecordingEventBroker` stub class that records events to a list.

### C0-3: `SaveAndLoad_RoundTrips_LayoutKind`

```csharp
[AvaloniaFact]
public void SaveAndLoad_RoundTrips_LayoutKind()
{
    var (manager, mdiHost) = CreateManager();
    var stub = new StubDockManager();
    manager.SetDockManager(stub);

    var window = new Window { Content = mdiHost, Width = 800, Height = 600 };
    window.Show();
    try
    {
        manager.SpawnPanel("PanelA", LayoutKind.Mdi);
        manager.SpawnPanel("PanelB", LayoutKind.DockDocument);

        var json = manager.SaveWorkspaceToJson();

        // Assert LayoutKind values are in the JSON
        Assert.Contains("\"LayoutKind\"", json);
        Assert.Contains("\"Mdi\"", json);
        Assert.Contains("\"DockDocument\"", json);
    }
    finally { window.Close(); }
}
```

---

## M1-T7 — ShellWindow Rebuild

Read `TASK-DETAILS.md#m1-t7--shellwindow-and-app-composition` in full.

### New `ShellWindow.axaml`

Replace the existing file with a structure matching DESIGN.md §3 layout sketch:

```
Window (Title="DDS Monitor", Width=1280, Height=820, MinWidth=800, MinHeight=500)
└─ DockPanel
   ├─ DockPanel.Dock="Top": Grid (3 columns: auto, auto, *)
   │   ├─ Col 0: Menu (4 items: _File, _View, _Devel, _Plugins)
   │   ├─ Col 1: StackPanel Horizontal (▶ ⏸ ⏹ transport buttons)
   │   └─ Col 2: StackPanel right-aligned (running indicator, TextBlock "Running/Paused", bandwidth, participant)
   ├─ DockPanel.Dock="Bottom": Border Height=24 (status bar TextBlock "Ready")
   └─ DockControl x:Name="MainDock" (fills remaining space)
```

**Menu structure (with mnemonics):**
- `_File`: `_Topic Sources…`, `_Plugin Manager…`, `—`, `_Reset Layout`, `_Export Layout…`, `_Import Layout…`, `—`, `E_xit`
- `_View`: populated dynamically via code-behind + fixed `_Theme ▶` submenu (`_System`, `_Light`, `_Dark`)
- `_Devel`: `_Enable Self-Sending` (checkable), `Self-Send _Rate ▶` submenu (`_1 Hz`, `_10 Hz`, `_100 Hz`, `1 _kHz`, `10 k_Hz`), `—`, `_Perf Stats…`
- `_Plugins`: populated dynamically

**Transport buttons** — inline in the top Grid column 1:
```xml
<Button x:Name="PlayButton"  Content="▶" ToolTip.Tip="Play (Space)"  IsDefault="False" />
<Button x:Name="PauseButton" Content="⏸" ToolTip.Tip="Pause (Space)" />
<Button x:Name="ResetButton" Content="⏹" ToolTip.Tip="Reset (F5)"   />
```

**Status indicator** — Grid column 2, right-aligned StackPanel:
```xml
<Ellipse x:Name="StatusDot" Width="10" Height="10" />
<TextBlock x:Name="StatusText" Text="Running" />
<TextBlock x:Name="BandwidthText" Text="0 B/s" Margin="8,0,0,0" />
```

### New `ShellWindow.axaml.cs`

Constructor takes `IServiceProvider services`:

```csharp
public ShellWindow(IServiceProvider services)
{
    InitializeComponent();
    // After component initialized:
    // 1. Create MdiHost
    // 2. Resolve IDockManager → Initialise(MainDock, mdiHost)
    // 3. Resolve IAvaloniaWindowManager → SetMdiHost(mdiHost); SetDockManager(dockManager)
    // 4. Rebuild View menu from IMenuRegistry
    // 5. Wire transport buttons
    // 6. Wire keyboard shortcuts (Space/F5/Ctrl+Tab)
    // 7. Start 1 Hz DispatcherTimer for status updates
}
```

**Bandwidth helper** (port from Blazor `MainLayout.razor`):
```csharp
private static string FormatBandwidth(long bps)
{
    if (bps <= 0) return "0 B/s";
    if (bps < 1024) return $"{bps} B/s";
    if (bps < 1_048_576) return $"{bps / 1024.0:0.##} KB/s";
    return $"{bps / 1_048_576.0:0.##} MB/s";
}
```

**Fix DESIGN.md §13 bugs 1 and 5** (these are in ShellWindow):
- Bug 1: toolbar buttons must use `Content=entry.Label` (or icon glyph from `entry.IconKey`) and `ToolTip.Tip=entry.Tooltip`
- Bug 5: remove the orphan `ContentArea Grid` — it no longer exists in the new AXAML

### Tests — `ShellWindowTests.cs`

Create `tests/DdsMonitor.Avalonia.Tests/ShellWindowTests.cs` with:

1. `ShellWindow_HasFourTopLevelMenuItems` — construct headless shell, assert Menu has 4 top-level items.
2. `TransportButtons_HaveToolTips` — verify Play/Pause/Reset buttons have non-empty `ToolTip.Tip`.
3. `BandwidthHelper_FormatsCorrectly` — unit test the `FormatBandwidth` helper (0 B/s, 512 B/s, 1.5 KB/s, 2.3 MB/s).
4. `ShellWindow_ActivePanels_EmptyOnStart` — on headless construction, `IWindowManager.ActivePanels` is empty.

---

## M1-T8 — Service Implementations

Read `TASK-DETAILS.md#m1-t8--service-implementations` in full.

All under `tools/DdsMonitor/DdsMonitor.Avalonia/Services/`.

### Files to create

1. **`ContextMenuPresenter.cs`** — implements `IContextMenuPresenter`
2. **`FileDialogService.cs`** — implements `IFileDialogService`
3. **`KeyboardShortcutService.cs`** — implements `IKeyboardShortcutService`
4. **`ThemeService.cs`** — implements `IThemeService`
5. **`ClipboardService.cs`** — implements `IClipboardService`

### Tests

Create under `tests/DdsMonitor.Avalonia.Tests/Services/`:

1. **`KeyboardShortcutServiceTests.cs`**: register a `KeyGesture`, call `TryInvoke`, assert action ran; `Registered` returns the entry.
2. **`ThemeServiceTests.cs`**: calling `SetMode(ThemeMode.Dark)` sets `Application.Current.RequestedThemeVariant == ThemeVariant.Dark` and fires `ModeChanged`.
3. **`ContextMenuPresenterTests.cs`**: stub `IContextMenuRegistry` returning 2 items; pass 1 default item; assert `ContextMenu.Items.Count == 3`.

### Key implementation notes

- `ThemeService` maps modes: `System → ThemeVariant.Default`, `Light → ThemeVariant.Light`, `Dark → ThemeVariant.Dark`.
- `FileDialogService` and `ClipboardService` need a `Func<TopLevel?>` lazy provider — don't call it in the constructor.
- `ContextMenuPresenter.Show` must call `ContextMenu.Open(anchor)` (Avalonia 11 API).
- `KeyboardShortcutService.TryInvoke` must dispatch action on UI thread via `Dispatcher.UIThread.Post`.

---

## M1-T9 — Program.cs DI + Bug Fixes

Read `TASK-DETAILS.md#m1-t9--programcs-di-and-bug-fixes` in full.

### `Program.cs` changes

1. Fix double `BuildAvaloniaApp` call (call it once, reuse).
2. Register all new services as singletons (see TASK-DETAILS.md §M1-T9 for full list).
3. Wire lazy providers: `FileDialogService(Func<Visual>)` and `ClipboardService(Func<TopLevel?>)` return the live main window.
4. Register `IWindowManager` as a factory pointing to the same `IAvaloniaWindowManager` singleton.

### Bug fixes

- **Bug §13 item 3**: `AvaloniaTypeDrawerRegistry.BuildFallback` — pass `converted` (not `box.Text`) to `ctx.OnChange`; catch on conversion failure.
- **Bug §13 item 4**: `TopicExplorerViewModel` — remove `RefreshTopics()` from constructor; keep only in `Initialize()`.
- **Bug §13 item 6** (auto-spawn): `TopicExplorerPlugin.Initialize` — remove the `windowManager.SpawnPanel(...)` call at the end.

### Tests — `ProgramRegistrationTests.cs`

Create `tests/DdsMonitor.Avalonia.Tests/ProgramRegistrationTests.cs`:

```csharp
[Fact]
public void AllNewServices_ResolveSuccessfully()
{
    var services = new ServiceCollection();
    // Register engine services (or their stubs)
    // Register all Avalonia services (same registrations as in Program.cs)
    var sp = services.BuildServiceProvider();

    Assert.NotNull(sp.GetService<IUiThreadInvoker>());
    Assert.NotNull(sp.GetService<IContextMenuPresenter>());
    Assert.NotNull(sp.GetService<IFileDialogService>());
    Assert.NotNull(sp.GetService<IKeyboardShortcutService>());
    Assert.NotNull(sp.GetService<IThemeService>());
    Assert.NotNull(sp.GetService<IClipboardService>());
    Assert.NotNull(sp.GetService<IDockManager>());
    Assert.NotNull(sp.GetService<IWindowManager>());
    Assert.NotNull(sp.GetService<IAvaloniaWindowManager>());
    Assert.Same(sp.GetService<IWindowManager>(), sp.GetService<IAvaloniaWindowManager>());
}
```

---

## Validation Steps

After completing all tasks:

```powershell
cd d:\Work\FastCycloneDdsCsharpBindings
dotnet build CycloneDDS.NET.sln -c Debug
dotnet test tests/DdsMonitor.Avalonia.Tests/ -c Debug
dotnet test tests/DdsMonitor.Avalonia.Core.Tests/ -c Debug
```

All three must succeed with **0 errors** and **0 failing tests**.

---

## Report

Write your completion report to `.dev-workstream/avalonia-2/reports/BATCH-04-REPORT.md`.

Answer these developer insight questions:

1. **C0 tests**: Did all 3 missing tests work cleanly? Was `RecordingEventBroker` straightforward to implement?
2. **ShellWindow + DockControl**: How did you wire the `DockControl.Layout` assignment to the `DdsDockFactory`'s layout in the code-behind? Was there any order-of-initialization issue?
3. **ThemeService**: How does `Application.Current.RequestedThemeVariant` interact with the `FluentTheme` in `App.axaml`? Does setting it to `Dark` at runtime actually switch the theme?
4. **Program.cs lazy providers**: How did you implement the `Func<TopLevel?>` for `FileDialogService` and `ClipboardService` so they return the main window after it's been created?
5. **TopicExplorerPlugin auto-spawn removal**: Did removing the SpawnPanel call break any existing tests?
6. **Weak points**: Any tests that couldn't be made fully behavioral due to headless constraints?
