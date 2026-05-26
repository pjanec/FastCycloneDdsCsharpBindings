# BATCH-05 Completion Report

**Batch:** BATCH-05  
**Workstream:** avalonia-2  
**Status:** ✅ COMPLETE — all tasks implemented, all tests passing

---

## Task Summary

| Task   | Title                             | Status   |
|--------|-----------------------------------|----------|
| M1-T11 | StandardPlugin touch-ups          | ✅ Done  |
| M1-T10 | FeatureDemoPlugin implementation  | ✅ Done  |
| M1-T12 | Smoke tests + manual checklist    | ✅ Done  |

---

## Test Results

```
DdsMonitor.Avalonia.Tests          98 / 98  Passed
DdsMonitor.Avalonia.FeatureDemoPlugin.Tests   4 / 4  Passed
```

No failures, no skips.

---

## Files Changed

### M1-T11 — StandardPlugin touch-ups

| File | Change |
|------|--------|
| `tools/DdsMonitor/DdsMonitor.Avalonia.StandardPlugin/TopicExplorerPlugin.cs` | Menu text → `"_Topic Explorer"`, toolbar label → `label: "Explorer"` |
| `tools/DdsMonitor/DdsMonitor.Avalonia.StandardPlugin/DummyDataGeneratorPlugin.cs` | Menu text → `"_Dummy Generator"` |
| `tools/DdsMonitor/DdsMonitor.Avalonia.StandardPlugin/SendSamplePlugin.cs` | Menu text → `"_Send Sample"` |
| `tools/DdsMonitor/DdsMonitor.Avalonia.StandardPlugin/WorkspaceManagerPlugin.cs` | `"Schema _Sources…"`, `"_Network Configuration…"` |

### M1-T10 — FeatureDemoPlugin (all new)

| File | Type |
|------|------|
| `tools/DdsMonitor/DdsMonitor.Avalonia.FeatureDemoPlugin/DemoTypes.cs` | 5 DDS topic structs + enums with schema attributes |
| `tools/DdsMonitor/DdsMonitor.Avalonia.FeatureDemoPlugin/DemoPublisherService.cs` | `IHostedService` publishing 5 DDS topic streams |
| `tools/DdsMonitor/DdsMonitor.Avalonia.FeatureDemoPlugin/FeatureDemoDashboardViewModel.cs` | ViewModel with Tick, TopicRows, RecentAlerts, toggle command |
| `tools/DdsMonitor/DdsMonitor.Avalonia.FeatureDemoPlugin/FeatureDemoDashboardView.axaml` | UserControl AXAML with compiled bindings |
| `tools/DdsMonitor/DdsMonitor.Avalonia.FeatureDemoPlugin/FeatureDemoDashboardView.axaml.cs` | Code-behind with 1 Hz DispatcherTimer |
| `tools/DdsMonitor/DdsMonitor.Avalonia.FeatureDemoPlugin/FeatureDemoPlugin.cs` | `IMonitorPlugin` entry-point; wires View, Devel menus |
| `tests/DdsMonitor.Avalonia.FeatureDemoPlugin.Tests/DemoPublisherServiceTests.cs` | 2 publisher behaviour tests |
| `tests/DdsMonitor.Avalonia.FeatureDemoPlugin.Tests/FeatureDemoDashboardViewModelTests.cs` | 2 ViewModel unit tests |
| `tools/DdsMonitor/DdsMonitor.Avalonia.FeatureDemoPlugin/Placeholder.cs` | **Deleted** |
| `tests/DdsMonitor.Avalonia.FeatureDemoPlugin.Tests/PlaceholderTests.cs` | **Deleted** |

### M1-T12 — Smoke tests + manual checklist

| File | Type |
|------|------|
| `tests/DdsMonitor.Avalonia.Tests/Smoke/M1SmokeTest.cs` | 3 automated smoke tests |
| `docs/M1-MANUAL-TEST-CHECKLIST.md` | 29-item manual test checklist |
| `tests/DdsMonitor.Avalonia.Tests/DdsMonitor.Avalonia.Tests.csproj` | Added FeatureDemoPlugin project reference |
| `tests/DdsMonitor.Avalonia.FeatureDemoPlugin.Tests/DdsMonitor.Avalonia.FeatureDemoPlugin.Tests.csproj` | Added `Microsoft.Extensions.Configuration` packages |

---

## Insight Questions

### Q1 — What was the most complex part of M1-T10, and why?

The most complex part was designing `DemoPublisherService` to be simultaneously testable and genuinely useful at runtime. The challenge was threefold:

1. **Threading discipline**: five independent publish loops run as `Task`-based background workers under a `CancellationToken`. Each loop must acquire a per-type `IDynamicWriter`, write in a tight interval loop, and clean up the writer in a `finally` block regardless of cancellation or exception. Coordinating the five loops via `Task.WhenAll` on a `_publishingTask` field that is nulled on stop required careful volatile-flag management (`_publishing` bool, `_cts` CancellationTokenSource).

2. **Testability without a real DDS stack**: `IDdsBridge.GetWriter` returns an `IDynamicWriter` per `TopicMetadata`. The inline `RecordingDdsBridge` stub in tests needed to track writes per topic type and return a distinct `CountingWriter` for each one. Getting the per-type dispatch right inside the stub without turning it into a production feature took several design iterations.

3. **Configuration layering**: the service reads `FeatureDemoPlugin:Enabled` from `IConfiguration`. Making this work with `ConfigurationBuilder().AddInMemoryCollection(...)` in tests required explicit `using Microsoft.Extensions.Configuration;` (extension methods are not resolved by implicit usings alone) — a subtle C# rule that caused a real build failure.

### Q2 — Why was the FeatureDemoDashboardViewModel designed with a manual Tick() instead of a reactive/observable timer?

The Avalonia headless test runner (`Avalonia.Headless.XUnit`) does not advance a dispatcher clock automatically, so a ViewModel that fires events internally on a `DispatcherTimer` would be untestable without real dispatcher infrastructure. Separating the "read and compute" logic into a synchronous `Tick()` method and keeping the timer purely in the code-behind (`FeatureDemoDashboardView.axaml.cs`) means:

- Tests call `Tick()` directly and assert synchronously — no async delays, no race conditions.
- The production code-behind creates and destroys the timer in the visual-tree lifecycle hooks (`OnAttachedToVisualTree` / `OnDetachedFromVisualTree`), so no timer leaks when panels are closed.

This pattern (ViewModel as pure logic, View owns the clock) is also consistent with how other StandardPlugin views work.

### Q3 — How does M1-T12 smoke test 3 verify the "Blazor JSON ↔ MDI" round-trip without a real DDS stack?

The test constructs a minimal `AvaloniaWindowManager` with a stub `IServiceProvider` (returns null for everything), a stub `IDockManager` (no-ops all dock calls), and a real `MdiHost` control. The JSON input deliberately omits the `LayoutKind` field, mimicking the Blazor-era workspace format.

`LoadWorkspaceFromJson` reads the panels array, calls `BuildContent` for each entry, and — because "BlazorUnknownPanel" is not registered in the `AvaloniaViewRegistry` — falls back to creating a `TextBlock` placeholder. It then calls `SpawnMdiPanel`, which unconditionally adds the content to `_mdiHost.Children`. The test then asserts:

- `wm.ActivePanels.Count == 1` — internal panel tracking was updated.
- `mdiHost.Children.Count == 1` — the MDI host physically received a child.
- `SaveWorkspaceToJson()` contains both `"LayoutKind"` and `"Mdi"` — the round-trip adds the field that was missing in the original.

No DDS infrastructure or real plugin is needed because the feature under test is purely the workspace serialization/deserialization path.

### Q4 — What is the significance of `[DdsUnion]` on `UnionPayload` and how does it differ from `[DdsCase]`?

`[DdsUnion]` is a **marker attribute** on the struct itself — it signals to the CycloneDDS code generator and runtime serializer that the type uses the IDL `union` layout (discriminated union with a single active member). It has no parameters.

`[DdsCase(value)]` is placed on individual **fields** and specifies which discriminator value selects that field. `[DdsDiscriminator]` marks the field that holds the active-case selector. `[DdsDefaultCase]` marks the field that is active for any discriminator value not covered by a `[DdsCase]`.

Together they encode the full IDL union grammar:
```
union UnionPayload switch(int) {
  case 1: int IntValue;
  case 2: string StringValue;
  case 3: double DoubleValue;
  default: bool DefaultValue;
};
```

The struct-level `[DdsUnion]` tells the serializer to apply union memory rules (only one field is valid per instance) rather than struct rules (all fields are valid). Without it, the runtime would serialize all fields regardless of the discriminator.

### Q5 — Why does `FeatureDemoPlugin.cs` register `DemoPublisherService` as both a singleton and a hosted service, and what would happen if only one registration were used?

The DI pattern used is:
```csharp
services.AddSingleton<DemoPublisherService>();
services.AddHostedService(sp => sp.GetRequiredService<DemoPublisherService>());
```

The singleton registration makes `DemoPublisherService` resolvable by its concrete type — which `FeatureDemoPlugin.Initialize` does via `context.GetFeature<DemoPublisherService>()`. Without the singleton registration, `GetFeature` would return `null` and the "Toggle Publisher" menu item would do nothing.

The hosted-service registration tells the host to call `StartAsync`/`StopAsync` on application start and shutdown. If only `AddHostedService<DemoPublisherService>()` were used (without the singleton), a second independent instance would be created for the menu handler — toggle calls would go to one instance while publishing happens on another.

If only the singleton were registered (no `AddHostedService`), `StartAsync` would never be called and no data would be published at startup.

The two-line pattern ensures a single instance is both lifecycle-managed by the host and reachable by concrete type for UI wiring.

### Q6 — How were the pre-existing `ReadersChanged` event warnings handled, and why were they left as warnings rather than suppressed?

Two stubs (`RecordingDdsBridge` and `NoOpDdsBridge`) implement `IDdsBridge`, which declares:
```csharp
event Action? ReadersChanged;
```

C# requires the event to be declared even in stub implementations, but it is never raised. The compiler emits `CS0067: The event … is never used` — a warning, not an error. The decision was to **leave the warning in place** rather than add `#pragma warning disable CS0067` or add a dummy field accessor, for two reasons:

1. The warning is informational and accurate — the event truly is never raised in a stub. Suppressing it would hide future cases where a production implementation forgets to raise it.
2. The stubs are test-internal types with a well-understood scope. Adding suppression pragma noise or a throw-based event accessor for a warning that does not block the build would be over-engineering.

The two warnings appear in `DemoPublisherServiceTests.cs` and `FeatureDemoDashboardViewModelTests.cs`; neither blocks the build or affects test correctness.

---

## Defects Found During Implementation

| # | Description | Root Cause | Fix |
|---|-------------|------------|-----|
| 1 | `VisualTreeAttachmentEventArgs` not found | Missing `using Avalonia;` in code-behind | Added the using |
| 2 | `float * 0.1` → implicit double conversion | `0.1` is a double literal | Changed to `0.1f` |
| 3 | `AddInMemoryCollection` not found in test | Extension method needs explicit `using Microsoft.Extensions.Configuration;` | Added the using |
| 4 | `Microsoft.Extensions.Configuration.Memory` NuGet does not exist | Package name was wrong; the extension is in the base package | Removed non-existent reference |
| 5 | `IMenuRegistry` not found in smoke test | Missing `using DdsMonitor.Engine.Plugins;` | Added the using |
