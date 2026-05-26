# Technical Debt Tracker — Avalonia-2 Workstream

Maintained by the Dev Lead. Updated after every batch review.

Legend:
- **P1** = Critical (blocks next batch; fixed inline as corrective tasks — never sits here)
- **P2** = Important (fix within next 2 batches)
- **P3** = Minor (fix when convenient)
- ✅ = Resolved

---

## Open Items

| ID | Priority | Description | Source | Target Batch |
|---|---|---|---|---|
| DEBT-001 | P2 | `PluginLoader_CorruptDll_DoesNotCrash` fails in Debug. `TryLoadPluginFromFile` catches exceptions only with `#if !DEBUG`. Always catch `BadImageFormatException` + `FileLoadException` (structural DLL issues) regardless of build config. | BATCH-01 review | BATCH-02 |
| DEBT-002 | P3 | `AvaloniaCoreSuite.cs` stub calls pass `"icon1"` to `Label` parameter (not `IconKey`) after `ToolbarEntry` positional reorder. Cosmetically wrong but tests don't assert on those fields. | BATCH-01 review | When next touching Core.Tests |
| DEBT-003 | P3 | `AvaloniaCoreSuite.cs` is growing large; `ToolbarRegistryTests` and `AvaloniaViewRegistryTests` should be split into dedicated files. | BATCH-01 review | When next touching test infra |

---

## Resolved Items

*(none yet)*
