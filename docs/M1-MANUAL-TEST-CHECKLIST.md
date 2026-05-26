# M1 Manual Test Checklist

Milestone M1 — Avalonia Shell Port (DDS Monitor)

Run these checks manually after a successful `dotnet build` and `dotnet test`. Mark each item ✅ Pass or ❌ Fail and note the build/date.

---

## Shell Startup

- [ ] **M1-001** Application launches without an unhandled exception dialog.
- [ ] **M1-002** The main window title bar shows **"DDS Monitor"**.
- [ ] **M1-003** The main menu bar contains exactly four top-level items: **File**, **View**, **Devel**, and **Plugins**.
- [ ] **M1-004** The **File** menu opens and shows **Topic Sources…**, **Plugin Manager…**, **Reset Layout**, **Export Layout…**, **Import Layout…**, and **Exit** in the correct order.
- [ ] **M1-005** Clicking **File → Exit** closes the application cleanly (no crash, no zombie process).

## Theme Switching

- [ ] **M1-006** **View → Theme → System** applies the OS-controlled light/dark theme without a restart.
- [ ] **M1-007** **View → Theme → Light** switches to a light palette immediately.
- [ ] **M1-008** **View → Theme → Dark** switches to a dark palette immediately.

## StandardPlugin — Keyboard Mnemonics

- [ ] **M1-009** Pressing **Alt** shows underlined mnemonic characters on all menu items that define them (e.g. `_Topic Explorer` underlines **T**, `_Network Configuration…` underlines **N**).
- [ ] **M1-010** **Tools → Schema _Sources…** (mnemonic **S**) opens the Schema Sources panel.
- [ ] **M1-011** **Tools → _Network Configuration…** (mnemonic **N**) opens the Network Configuration panel.
- [ ] **M1-012** **Tools → _Dummy Generator** (mnemonic **D**) starts or stops the dummy data generator.
- [ ] **M1-013** **Tools → _Send Sample** (mnemonic **S**) opens the Send Sample panel.
- [ ] **M1-014** **View → _Topic Explorer** (mnemonic **T**) opens the Topic Explorer panel and the toolbar shows an **"Explorer"** button.

## FeatureDemo Plugin — Dashboard

- [ ] **M1-015** **View → Feature _Demo Dashboard** opens the Feature Demo Dashboard panel inside the MDI host.
- [ ] **M1-016** The dashboard panel shows a **"Publishing"** button after startup (publisher starts automatically).
- [ ] **M1-017** Clicking the **"Publishing"** button switches the label to **"Stopped"** and stops publishing.
- [ ] **M1-018** Clicking **"Stopped"** resumes publishing and the label switches back to **"Publishing"**.
- [ ] **M1-019** The topic-count table updates every second and shows five rows: `Telemetry`, `EntityState`, `Alert`, `GeoLocation`, `UnionPayload`.
- [ ] **M1-020** Telemetry count increments at least 5 times within 5 seconds (≥ 1 Hz effective).
- [ ] **M1-021** **Devel → _Feature Demo Toggle Publisher** also toggles the publisher state in sync with the dashboard label.

## MDI Behaviour

- [ ] **M1-022** Panels open inside the MDI host (not as separate OS windows).
- [ ] **M1-023** MDI panels can be **dragged** to a new position on the canvas.
- [ ] **M1-024** MDI panels can be **resized** by dragging the resize handle.
- [ ] **M1-025** Clicking the **✕** on an MDI panel closes it and removes it from the active panels list (verified via status bar or panel count).
- [ ] **M1-026** Minimising an MDI panel moves it to the minimised strip at the bottom; restoring returns it to the canvas.

## Workspace Round-trip

- [ ] **M1-027** **File → Export Layout…** saves a JSON file that contains at least the `Panels` and `LayoutKind` keys.
- [ ] **M1-028** Opening an exported layout via **File → Import Layout…** restores the same panels in the same MDI positions.
- [ ] **M1-029** Importing a Blazor-era workspace JSON file (without the `LayoutKind` field) opens all listed panels as MDI windows without errors.

---

*Tester:* _______________  
*Build / date:* _______________  
*Result summary:* ___  pass  /  ___  fail  /  ___  skipped
