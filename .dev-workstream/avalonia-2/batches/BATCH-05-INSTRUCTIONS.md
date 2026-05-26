# BATCH-05 Instructions

**Branch:** `ddsmon-avalonia`  
**Previous batch:** BATCH-04 (APPROVED ✅)  
**Tasks:** M1-T10 + M1-T11 + M1-T12  
**Prerequisite reading:** `DESIGN.md` end-to-end, `TASK-DETAILS.md` sections for M1-T10, M1-T11, M1-T12, then this file.

---

## Context Summary

All M1 tasks T0–T9 are complete. The shell runs with:
- DockControl with MDI Workspace document containing an `MdiHost`
- 5 Avalonia service implementations
- Full DI wiring in `Program.cs`
- AvaloniaWindowManager routing to MDI, DockDocument, DockTool

This batch delivers the remaining M1 tasks:
- **M1-T10**: FeatureDemoPlugin — 5 DDS topic types, publisher service, dashboard panel
- **M1-T11**: StandardPlugin sweep — toolbar `label`, menu mnemonics, no auto-spawn audit
- **M1-T12**: E2E smoke tests + manual checklist doc

---

## Task Order

1. **M1-T11** first (compile sweep — smallest, zero-risk)
2. **M1-T10** (new plugin — self-contained)
3. **M1-T12** (smoke tests — depends on everything)

---

## M1-T11 — StandardPlugin Touch-ups

### Scope
Refer to `TASK-DETAILS.md` M1-T11 for authoritative spec. Summary:

1. **Toolbar registrations**: The new `IToolbarRegistry.Register` signature is `Register(string id, Action onClick, string label = "", string tooltip = "", string? iconKey = null)`. In `TopicExplorerPlugin.cs` the call uses named parameters so it still compiles. However, the `label` field is empty. **Add `label: "Explorer"` (or similar short text)** to each toolbar registration in StandardPlugin. Also check `SendSamplePlugin.cs`, `DummyDataGeneratorPlugin.cs`, and `WorkspaceManagerPlugin.cs` for any toolbar registrations.

2. **Menu mnemonics**: Update all `AddMenuItem` call sites that lack underscore-prefixed mnemonics:
   - `"Topic Explorer"` → `"_Topic Explorer"`
   - `"Schema Sources…"` → `"Schema _Sources…"`
   - `"Network Configuration…"` → `"_Network Configuration…"`
   - `"Dummy Generator"` → `"_Dummy Generator"`
   - `"Send Sample"` → `"_Send Sample"`
   (Verify no duplicate `_` letter within the same menu parent.)

3. **Auto-spawn audit**: Confirm no `*Plugin.cs` calls `windowManager.SpawnPanel(...)` from within `Initialize` itself. `TopicExplorerPlugin` was already fixed in BATCH-04. Verify all others.

### Acceptance
- `dotnet build CycloneDDS.NET.sln -c Debug` → 0 errors
- `dotnet test tests/DdsMonitor.Avalonia.StandardPlugin.Tests/ -c Debug` → 0 failures

---

## M1-T10 — FeatureDemoPlugin

### Scope
Refer to `TASK-DETAILS.md` M1-T10 for authoritative spec. Summary:

**1. Delete** `tools/DdsMonitor/DdsMonitor.Avalonia.FeatureDemoPlugin/Placeholder.cs`

**2. Create** `DemoTypes.cs` — all topic structs:

```csharp
// tools/DdsMonitor/DdsMonitor.Avalonia.FeatureDemoPlugin/DemoTypes.cs
using DdsMonitor.Engine;      // for TopicMetadata attribute lookup
using CycloneDDS.Core;        // for DdsTopic, DdsKey — search the Engine/Runtime for actual usings

namespace DdsMonitor.Avalonia.FeatureDemoPlugin;

// Telemetry - unkeyed, fast (10 Hz)
[DdsTopic("FeatureDemo/Telemetry")]
public struct TelemetrySample
{
    public long Timestamp;
    public int SequenceId;
    public double Cpu;
    public double Memory;
    public float Temperature;
}

// Entity state - keyed (5 Hz)
[DdsTopic("FeatureDemo/EntityState")]
public struct EntityState
{
    [DdsKey] public int EntityId;
    public string Name;
    public EntityKind Kind;
    public float X, Y, Z;
    public byte Health;
    public bool IsAlive;
}

public enum EntityKind { Player, Npc, Vehicle, Projectile }

// Alert - unkeyed, slow
[DdsTopic("FeatureDemo/Alert")]
public struct AlertEvent
{
    public long Timestamp;
    public Severity Level;
    public string Message;
    public string Origin;
}

public enum Severity { Info, Warning, Error, Critical }

// Geo location - unkeyed, nested
[DdsTopic("FeatureDemo/GeoLocation")]
public struct GeoLocation
{
    public double Latitude;
    public double Longitude;
    public float Altitude;
    public Address NestedAddress;
}

public struct Address
{
    public string Street;
    public string City;
    public string Country;
}

// Union payload - unkeyed, demonstrates union
[DdsTopic("FeatureDemo/UnionPayload")]
public struct UnionPayload
{
    public int Discriminator;
    public int IntValue;
    public string StringValue;
    public double DoubleValue;
    public bool DefaultValue;
}
```

> **IMPORTANT**: Check actual attribute names before writing. Look at `HeartbeatSample.cs` in StandardPlugin and search `CycloneDDS.Schema` / `DdsMonitor.Engine` for the real `DdsTopic`/`DdsKey` attributes. Use the **exact same** attribute classes and `using` namespaces as `HeartbeatSample.cs`. If the attributes don't exist on plain structs (e.g., they require IDL code-gen), model the topic types after `HeartbeatSample` instead and skip `[DdsKey]` if the engine doesn't support it at registration time.

**3. Create** `DemoPublisherService.cs`:
- Implements `IHostedService, IDisposable`
- Inject: `ITopicRegistry`, `IDdsBridge`, `IConfiguration`, `ILogger<DemoPublisherService>?`
- Model on `DummyGeneratorService` from `DdsMonitor.Avalonia.StandardPlugin`
- `StartAsync`: registers all 5 topic types via `ITopicRegistry.Register(new TopicMetadata(typeof(T)))`, starts background tasks if `FeatureDemoPlugin:Enabled = true` (default)
- Five publish loops (each in its own `Task`):
  - Telemetry: 100ms tick, random CPU/mem/temp with ±2% drift from previous
  - EntityState: 200ms tick, 8 entities with keys 1–8; position random walk ±1 per tick
  - Alert: 7000ms tick, cycling through Severity values
  - GeoLocation: 2000ms tick, random walk lat/lon in Europe range (45–55°N, 0–20°E)
  - UnionPayload: 3000ms tick, cycling discriminator 1→2→3→0
- `ToggleEnabled()` public method that starts/stops publishing
- Use `_random = new Random(42)` for deterministic test output
- Stop cleanly on cancellation

**4. Create** `FeatureDemoDashboardViewModel.cs`:
- Inject: `ISampleStore`, `DemoPublisherService`
- Properties: `string PublisherStateLabel { get; }` ("Publishing" / "Stopped")
- `IReadOnlyList<TopicCountRow> TopicRows { get; }` — one row per topic type
- `ObservableCollection<string> RecentAlerts { get; }` — last 10 alert strings
- `ICommand TogglePublisherCommand`
- Method `void Tick()` called by a 1 Hz timer from the view — updates `TopicRows` by calling `ISampleStore.GetTopicCount(typeof(TelemetrySample))` etc.
- `TopicCountRow` co-located record: `record TopicCountRow(string Name, int Count)`
- Implement `IStatefulViewModel` (`Initialize(IDictionary<string, object>)`) — calls `Tick()` once on init

**5. Create** `FeatureDemoDashboardView.axaml` + `FeatureDemoDashboardView.axaml.cs`:
- AXAML: `UserControl` layout with `Grid RowDefinitions="Auto,Auto,*"`:
  - Row 0: `Button` bound to `TogglePublisherCommand`, `Content="{Binding PublisherStateLabel}"`
  - Row 1: `DataGrid` or 5-row `ItemsControl` bound to `TopicRows`, columns "Topic" + "Count"
  - Row 2: `ListBox` bound to `RecentAlerts`
- Code-behind: start a 1 Hz `DispatcherTimer` on `OnAttachedToVisualTree`, stop on `OnDetachedFromVisualTree`, each tick calls `ViewModel.Tick()`

**6. Create** `FeatureDemoPlugin.cs`:
- Implements `IMonitorPlugin`
- `Name = "FeatureDemo"`, `Version = "1.0"`
- `ConfigureServices(IServiceCollection services)`:
  - `services.AddSingleton<DemoPublisherService>()`
  - `services.AddHostedService(sp => sp.GetRequiredService<DemoPublisherService>())`
- `Initialize(IMonitorContext context)`:
  - Get `IMenuRegistry`, `IWindowManager`, `IAvaloniaViewRegistry`, `DemoPublisherService` from `context.Services`
  - `viewRegistry.Register<FeatureDemoDashboardViewModel>(vm => new FeatureDemoDashboardView { DataContext = vm })`
  - `menuRegistry.AddMenuItem("Devel", "_Feature Demo Toggle Publisher", () => context.Services.GetRequiredService<DemoPublisherService>().ToggleEnabled())`
  - `menuRegistry.AddMenuItem("View", "Feature _Demo Dashboard", () => windowManager.SpawnPanel(nameof(FeatureDemoDashboardViewModel), null))`

### Tests

**7. Create** `tests/DdsMonitor.Avalonia.FeatureDemoPlugin.Tests/DemoPublisherServiceTests.cs`:

The test project doesn't reference `Avalonia.Headless.XUnit`, so use plain `[Fact]` tests.

Test: `DemoPublisherService_Publishes_AllFiveTopics`:
- Create fake `ITopicRegistry` (just stores what was registered)
- Create fake `IDdsBridge` that returns a fake `IDynamicWriter` per topic, records `Write` calls
- Create an `IConfiguration` with `FeatureDemoPlugin:Enabled = true`
- Construct `DemoPublisherService`
- Call `StartAsync(CancellationToken.None)`
- `await Task.Delay(500)` (500ms — enough for at least one publish cycle at 100ms)
- Call `StopAsync(CancellationToken.None)`
- Assert: fake `ITopicRegistry` received 5 distinct `Register` calls (one per topic type)
- Assert: fake writer for `TelemetrySample` received at least 1 `Write` call

Test: `DemoPublisherService_ToggleEnabled_StopsPublishing`:
- Start service (enabled)
- `await Task.Delay(200)`
- `service.ToggleEnabled()` (should stop)
- Record write count at stop
- `await Task.Delay(300)`
- Assert write count did not increase significantly after stop

**8. Create** `tests/DdsMonitor.Avalonia.FeatureDemoPlugin.Tests/FeatureDemoDashboardViewModelTests.cs`:

Test: `TopicRows_ReflectSampleStoreCount`:
- Fake `ISampleStore` returns count 5 for `TelemetrySample`, 10 for `EntityState`, etc.
- Construct `FeatureDemoDashboardViewModel(fakeSampleStore, fakeDemoPublisherService)`
- Call `Tick()`
- Assert `TopicRows.First(r => r.Name.Contains("Telemetry")).Count == 5`
- Assert `TopicRows.First(r => r.Name.Contains("Entity")).Count == 10`

Test: `PublisherStateLabel_ReflectsToggle`:
- Construct VM
- Assert label is "Stopped" (since test service is not running)
- (Or vice versa depending on default)

### Acceptance
- `dotnet build CycloneDDS.NET.sln -c Debug` → 0 errors
- `plugins/DdsMonitor.Avalonia.FeatureDemoPlugin.dll` exists after build (StagePlugin target)
- `dotnet test tests/DdsMonitor.Avalonia.FeatureDemoPlugin.Tests/ -c Debug` → 0 failures
- `dotnet test tests/DdsMonitor.Avalonia.Tests/ -c Debug` → still 95/95 (no regressions)

---

## M1-T12 — M1 End-to-End Smoke Test + Manual Checklist

### Scope
Refer to `TASK-DETAILS.md` M1-T12 for authoritative spec.

**1. Create** `tests/DdsMonitor.Avalonia.Tests/Smoke/M1SmokeTest.cs`:

The test project has `Avalonia.Headless.XUnit` so use `[AvaloniaFact]` where headless window is needed.

> **Design constraint**: The smoke test cannot use real DDS (native runtime) in CI. Use stubs for `IDdsBridge`, `ISampleStore`, `ITopicRegistry` in the DI graph.

Test 1: `M1_ColdStart_ProducesEmptyShell`:
```csharp
[AvaloniaFact]
public void M1_ColdStart_ProducesEmptyShell()
{
    // Build minimal service provider (same pattern as ShellWindowTests)
    var viewRegistry = new AvaloniaViewRegistry();
    var eventBroker  = new StubEventBroker();
    var sp = new ServiceCollection()
        .AddSingleton<IAvaloniaViewRegistry>(viewRegistry)
        .AddSingleton<IEventBroker>(eventBroker)
        .AddSingleton<IAvaloniaWindowManager>(s =>
            new AvaloniaWindowManager(viewRegistry, s, eventBroker))
        .AddSingleton<IWindowManager>(s =>
            (IWindowManager)s.GetRequiredService<IAvaloniaWindowManager>())
        .AddSingleton<IDockManager, DockManager>()
        // ... other minimal stubs
        .BuildServiceProvider();

    var window = new ShellWindow(sp);
    var menu = window.FindControl<Menu>("MainMenu");
    var wm   = sp.GetRequiredService<IAvaloniaWindowManager>();

    Assert.Equal(4, menu!.Items.Count);
    Assert.Empty(wm.ActivePanels);
    Assert.Equal("DDS Monitor", window.Title);
}
```

Test 2: `M1_FeatureDemo_RegistersFiveTopicTypes`:
```csharp
[Fact]
public void M1_FeatureDemo_RegistersFiveTopicTypes()
{
    // Use fake ITopicRegistry that collects registrations
    var fakeRegistry = new CapturingTopicRegistry();
    var fakeBridge   = new NoOpDdsBridge();
    var config       = new ConfigurationBuilder()
        .AddInMemoryCollection(new[] {
            KeyValuePair.Create("FeatureDemoPlugin:Enabled", "false") })
        .Build();

    var svc = new DemoPublisherService(fakeRegistry, fakeBridge, config);
    svc.StartAsync(CancellationToken.None).GetAwaiter().GetResult();
    svc.StopAsync(CancellationToken.None).GetAwaiter().GetResult();

    Assert.Equal(5, fakeRegistry.Registrations.Select(r => r.TopicType).Distinct().Count());
}
```

Test 3: `M1_WorkspaceRoundtrip_BlazorJsonLoadsAsMdi`:
```csharp
[AvaloniaFact]
public void M1_WorkspaceRoundtrip_BlazorJsonLoadsAsMdi()
{
    // Use AvaloniaWindowManagerDockTests pattern: real MdiHost + StubDockManager
    // Build the manager
    var blazorJson = """{"Panels":[{"ComponentType":"TopicExplorerViewModel","ComponentState":{"__window":[80,60,600,400]}}]}""";

    var viewRegistry = new AvaloniaViewRegistry();
    viewRegistry.Register<TopicExplorerViewModel>(vm => new TextBlock { Tag = vm });
    var eventBroker = new StubEventBroker();
    var stubDock    = new StubDockManager();
    var sp          = new ServiceCollection()
        /* ... minimal stubs */ .BuildServiceProvider();

    var wm   = new AvaloniaWindowManager(viewRegistry, sp, eventBroker);
    var host = new MdiHost();
    wm.SetMdiHost(host);
    wm.SetDockManager(stubDock);

    wm.LoadWorkspaceFromJson(blazorJson);

    // Should spawn the panel as MDI (Blazor JSON has no LayoutKind field)
    Assert.Single(wm.ActivePanels);
    Assert.Equal(1, host.Children.Count);

    // Save and verify round-trip JSON has LayoutKind
    var saved = wm.SaveWorkspaceToJson();
    Assert.Contains("LayoutKind", saved);
    Assert.Contains("Mdi", saved);
}
```

> **Note**: `StubDockManager`, `CapturingTopicRegistry`, `NoOpDdsBridge` are internal test helpers in the same file. Reuse any existing stubs from `AvaloniaWindowManagerTests.cs` where possible (same class or nested private class). `TopicExplorerViewModel` may need minimal constructor args — stub them or create a `FakeTopicExplorerViewModel` that doesn't need real engine services.

**2. Create** `docs/M1-MANUAL-TEST-CHECKLIST.md`:

```markdown
# M1 Manual Test Checklist

Run these after every M1 build on a developer machine (Windows). ~15 minutes.

## Prerequisites
- DDS daemon running (or Stub mode via `appsettings.json` `DdsSettings.HeadlessMode = Replay`)
- `dotnet build CycloneDDS.NET.sln -c Debug` → 0 errors

## Shell Startup
- [ ] App launches without errors or crash dialogs
- [ ] Window title = "DDS Monitor"
- [ ] Menu bar has exactly 4 top-level items: File, View, Devel, Plugins
- [ ] File menu: mnemonics work (Alt+F opens File, then F for Topic Sources)
- [ ] Transport buttons ▶ ⏸ ⏹ have tooltips visible on hover
- [ ] Status indicator dot is visible (green = running, orange = paused)
- [ ] Status bar at bottom shows "Ready"
- [ ] Bandwidth TextBlock shows "0 B/s" with no DDS traffic

## Theme Switching
- [ ] View → Theme → Dark switches entire window to dark theme
- [ ] View → Theme → Light reverts to light
- [ ] View → Theme → System follows OS setting
- [ ] Theme preference persists on restart (requires `IUserSettings` save working)

## FeatureDemo Plugin
- [ ] Devel → Feature Demo Toggle Publisher → starts publishing (verify in Devel → Perf Stats or log output)
- [ ] View → Feature Demo Dashboard → opens MDI child with dashboard
- [ ] Dashboard shows 5 topic rows (Telemetry, EntityState, Alert, GeoLocation, UnionPayload)
- [ ] Counts increase over time while publisher is active
- [ ] Toggle again → publishing stops → counts freeze
- [ ] Recent Alerts list populates every ~7 seconds

## MDI Host Behaviour
- [ ] Feature Demo Dashboard can be dragged by its titlebar
- [ ] Dashboard can be resized from any edge/corner
- [ ] Titlebar right-click shows context menu with 7 items (Dock as tab, 3 dock-as-tool, separator, Minimise, Close)
- [ ] Minimise button `_` collapses the window to the bottom strip
- [ ] Clicking the strip button restores the window
- [ ] Close ✕ removes the child from the host

## Workspace Round-trip
- [ ] File → Reset Layout asks for confirmation and clears MDI + dock
- [ ] Open a Feature Demo Dashboard, resize it, close app, reopen → position/size preserved

## StandardPlugin Touch-ups
- [ ] View → _Topic Explorer (mnemonic works)
- [ ] Tools menu (if present) shows Schema _Sources, _Network Configuration with mnemonics
- [ ] No panels auto-open on startup (unless a workspace file re-opens them)
```

### Acceptance
- `dotnet build CycloneDDS.NET.sln -c Debug` → 0 errors
- `dotnet test tests/DdsMonitor.Avalonia.Tests/ -c Debug` → 0 failures (≥ 98 tests)
- `dotnet test tests/DdsMonitor.Avalonia.FeatureDemoPlugin.Tests/ -c Debug` → 0 failures

---

## Non-obvious Implementation Notes

### Fake IDdsBridge for tests
The real `IDdsBridge` requires native DDS binaries. For tests, look for an existing `StubDdsBridge` or `FakeDdsBridge` in the test suites — search for `IDdsBridge` in `tests/`. If none exists, create a minimal one:

```csharp
private sealed class NoOpDdsBridge : IDdsBridge
{
    // Return a fake IDynamicWriter that counts Write calls
    public IDynamicWriter GetWriter(TopicMetadata meta) => new RecordingWriter();
    // Other IDdsBridge members throw NotImplementedException or return defaults
    ...
}
private sealed class RecordingWriter : IDynamicWriter
{
    public int WriteCount;
    public void Write(object sample) => Interlocked.Increment(ref WriteCount);
}
```

> Check `IDdsBridge` interface in `tools/DdsMonitor/DdsMonitor.Engine/` to see all members before writing the stub.

### FeatureDemoPlugin: IMonitorPlugin interface
Look up `IMonitorPlugin` in `tools/DdsMonitor/DdsMonitor.Engine/Plugins/` to see the exact contract. Also check `IMonitorContext` for what services `Initialize` can resolve.

### TopicExplorerViewModel constructor args
`TopicExplorerViewModel` requires `ITopicRegistry`, `IContextMenuRegistry`, `IEventBroker`, `IUserSettings`. For smoke test 3 (`BlazorJsonLoadsAsMdi`), either:
- Register a `TopicExplorerViewModel` factory in the SP that uses `ActivatorUtilities`, or
- Replace the registered view factory with a stub: `viewRegistry.Register<TopicExplorerViewModel>(_ => new TextBlock())` to avoid needing real services for the VM.

The second option is simpler and already used in BATCH-03's tests.

### DdsTopic/DdsKey attribute lookup
Before writing DemoTypes.cs, run:
```
grep -r "\[DdsTopic\]\|\[DdsKey\]" tools/DdsMonitor/ src/
```
to find the exact attribute class and namespace. Use those. `HeartbeatSample.cs` in StandardPlugin is the primary reference.

---

## Success Criteria (ALL must pass)

- `dotnet build CycloneDDS.NET.sln -c Debug` → 0 errors
- `dotnet test tests/DdsMonitor.Avalonia.Tests/ -c Debug` → 0 failures
- `dotnet test tests/DdsMonitor.Avalonia.Core.Tests/ -c Debug` → 0 failures  
- `dotnet test tests/DdsMonitor.Avalonia.FeatureDemoPlugin.Tests/ -c Debug` → 0 failures
- `tools/DdsMonitor/DdsMonitor.Avalonia/bin/Debug/net8.0/plugins/DdsMonitor.Avalonia.FeatureDemoPlugin.dll` exists
- `Placeholder.cs` is deleted

---

## Developer Report

Write report to `.dev-workstream/avalonia-2/reports/BATCH-05-REPORT.md`.

Answer all 6 insight questions:
1. DemoTypes attributes — what were the exact attribute names/namespaces found, and did you need to model differently from the TASK-DETAILS spec?
2. DemoPublisherService — how is the 5-topic publish loop structured? Any ordering or cancellation issues?
3. FeatureDemoDashboard — how does the 1 Hz timer interact with the `ObservableCollection`? Thread safety?
4. M1-T11 — which menu items already had mnemonics vs which needed adding?
5. Smoke tests — what stubs were reused from existing test files? What new stubs were needed?
6. Any weak points in the tests? (Headless limitations, timing sensitivity, etc.)
