# Runtime UI Verification Report

## Purpose

This report proves how the actual WPF simulator UI moves during debug/capture mode. The screenshots are rendered from the same `MachineTwinView` and `MachineTwinViewModel` used by the running app.

## Execution Command

```powershell
dotnet run --project src/SemiTool.Hmi.Wpf/SemiTool.Hmi.Wpf.csproj --configuration Release -- --capture-ui-debug-report
```

## Verification Boundary

- Simulator mode only.
- No vendor DLL is loaded.
- No real hardware connection is attempted.
- Visual theta angle is for HMI rendering only.
- Preserved theta encoder values are machine/teaching values, not literal UI degrees.
- The robot is modeled as a limited station-to-station theta swing, not continuous 360-degree rotation.
- Normal runtime `Run Teaching Demo` holds at FOUP B 5/5 completed until the user presses Reset; only explicit capture modes call application shutdown.

## Runtime Integration Check

- MainWindow first tab is `Machine Twin`.
- MainWindow uses `<views:MachineTwinView DataContext="{Binding MachineTwin}" />`.
- MainViewModel exposes `MachineTwinViewModel` through the `MachineTwin` property.
- `Run Teaching Demo` is a command on the actual `MachineTwinView` runtime screen.
- `00-startup-simulator.png` is captured from the actual `MainWindow`, so it shows the selected `Machine Twin` tab.
- The remaining screenshots are captured from the same `MachineTwinView` and `MachineTwinViewModel` used by the running app.

## Captured Steps

| Step | State | Action | Station | FOUP A | FOUP B | Chambers | Door/Blade/Vacuum | Screenshot |
|---:|---|---|---|---:|---:|---|---|---|
| 0 | Startup Simulator | Pipeline ready: FOUP A 5 wafers, FOUP B empty | FOUP A | 5/5 | 0/5 | Chamber A:Empty::PreClean_Default:-:0s:0%<br>Chamber B:Empty::CMP_Main:-:0s:0%<br>Chamber C:Empty::PostClean_Dry:-:0s:0% | A:Closed B:Closed C:Closed<br>Retracted<br>Off | [00-startup-simulator.png](screenshots/00-startup-simulator.png) |
| 1 | Move To FOUP A Slot A1 | Moving to FOUP A Slot A1 | FOUP A | 5/5 | 0/5 | Chamber A:Empty::PreClean_Default:-:0s:0%<br>Chamber B:Empty::CMP_Main:-:0s:0%<br>Chamber C:Empty::PostClean_Dry:-:0s:0% | A:Closed B:Closed C:Closed<br>Retracted<br>Off | [01-foup-a-before-pickup.png](screenshots/01-foup-a-before-pickup.png) |
| 4 | W01 On Blade From FOUP A Slot A1 | W01 picked onto blade | FOUP A | 4/5 | 0/5 | Chamber A:Empty::PreClean_Default:-:0s:0%<br>Chamber B:Empty::CMP_Main:-:0s:0%<br>Chamber C:Empty::PostClean_Dry:-:0s:0% | A:Closed B:Closed C:Closed<br>Extended<br>SuctionOn | [02-blade-holding-wafer-after-pickup.png](screenshots/02-blade-holding-wafer-after-pickup.png) |
| 8 | Chamber A Door Opening | Open Chamber A door before loading W01 | Chamber A | 4/5 | 0/5 | Chamber A:Empty::PreClean_Default:-:0s:0%<br>Chamber B:Empty::CMP_Main:-:0s:0%<br>Chamber C:Empty::PostClean_Dry:-:0s:0% | A:Opening B:Closed C:Closed<br>Retracted<br>SuctionOn | [03-chamber-a-door-opening.png](screenshots/03-chamber-a-door-opening.png) |
| 10 | Blade Entering Chamber A | Blade extending into Chamber A | Chamber A | 4/5 | 0/5 | Chamber A:Empty::PreClean_Default:-:0s:0%<br>Chamber B:Empty::CMP_Main:-:0s:0%<br>Chamber C:Empty::PostClean_Dry:-:0s:0% | A:Open B:Closed C:Closed<br>Extending<br>SuctionOn | [04-blade-entering-chamber-a-door-open.png](screenshots/04-blade-entering-chamber-a-door-open.png) |
| 12 | W01 Placed At Chamber A | W01 placed at Chamber A | Chamber A | 4/5 | 0/5 | Chamber A:Loaded:W01:PreClean_Default:Chem Clean:0s:0%<br>Chamber B:Empty::CMP_Main:-:0s:0%<br>Chamber C:Empty::PostClean_Dry:-:0s:0% | A:Open B:Closed C:Closed<br>Extended<br>ExhaustOrRelease | [05-wafer-placed-chamber-a-stage.png](screenshots/05-wafer-placed-chamber-a-stage.png) |
| 13 | Blade Retracting Empty From Chamber A | Blade retracting empty from Chamber A | Chamber A | 4/5 | 0/5 | Chamber A:Loaded:W01:PreClean_Default:Chem Clean:0s:0%<br>Chamber B:Empty::CMP_Main:-:0s:0%<br>Chamber C:Empty::PostClean_Dry:-:0s:0% | A:Open B:Closed C:Closed<br>Retracting<br>Off | [06-blade-retracted-before-chamber-a-door-closes.png](screenshots/06-blade-retracted-before-chamber-a-door-closes.png) |
| 17 | Chamber A Processing W01 | Chamber A processing W01 | Chamber A | 4/5 | 0/5 | Chamber A:Processing:W01:PreClean_Default:Chem Clean:4s:10%<br>Chamber B:Empty::CMP_Main:-:0s:0%<br>Chamber C:Empty::PostClean_Dry:-:0s:0% | A:Closed B:Closed C:Closed<br>Retracted<br>Off | [07-chamber-a-processing-door-closed.png](screenshots/07-chamber-a-processing-door-closed.png) |
| 22 | Blade Extending Into Chamber A | Blade extending into Chamber A | Chamber A | 4/5 | 0/5 | Chamber A:Completed:W01:PreClean_Default:Chem Clean:0s:100%<br>Chamber B:Empty::CMP_Main:-:0s:0%<br>Chamber C:Empty::PostClean_Dry:-:0s:0% | A:Open B:Closed C:Closed<br>Extending<br>Off | [08-chamber-a-unload-after-process-complete.png](screenshots/08-chamber-a-unload-after-process-complete.png) |
| 391 | FOUP B 5 Wafers Complete | All 5 wafers complete in FOUP B | FOUP B | 0/5 | 5/5 | Chamber A:Empty::PreClean_Default:-:0s:0%<br>Chamber B:Empty::CMP_Main:-:0s:0%<br>Chamber C:Empty::PostClean_Dry:-:0s:0% | A:Closed B:Closed C:Closed<br>Retracted<br>Off | [09-final-foup-b-5-completed.png](screenshots/09-final-foup-b-5-completed.png) |
| 392 | Reset Safe State | Reset to safe simulator state | FOUP A | 5/5 | 0/5 | Chamber A:Empty::PreClean_Default:-:0s:0%<br>Chamber B:Empty::CMP_Main:-:0s:0%<br>Chamber C:Empty::PostClean_Dry:-:0s:0% | A:Closed B:Closed C:Closed<br>Retracted<br>Off | [10-reset-safe-state.png](screenshots/10-reset-safe-state.png) |

## Expected vs Actual Movement

| Expected simulator movement | Evidence in this report |
|---|---|
| Machine Twin starts in Simulator mode and does not connect to real hardware. | Step 0 shows `IsSimulatorMode=true` and `IsRealHardwareMode=false`; `IsConnected` refers to the simulator controller connection, not real equipment. |
| FOUP A starts with five wafers. | Steps 0 and 1 show `FoupACount=5` and `FoupBCount=0`. |
| Theta target follows the limited station arc instead of a 360-degree dial. | The trace records station-to-station `ThetaTargetName` changes plus preserved encoder values. |
| Z moves from Safe to Work only during pick/place visualization. | Pick/place steps show `ZState=Z Work`; processing and reset states return to `Z Safe`. |
| Chamber doors gate blade entry. | Chamber-target blade-extension steps include `DoorState=Open`; close steps occur only after the blade retracts. |
| Cylinder forward extends the telescopic blade. | Steps with `BladeTeachingState=Extending/Extended` also show `IsCylinderForward=true`. |
| Vacuum suction attaches the wafer to the blade. | Pickup steps show `VacuumDisplayState=SuctionOn` before the wafer appears on the blade. |
| Vacuum exhaust/release places the wafer into the chamber or FOUP. | Placement steps show `VacuumDisplayState=ExhaustOrRelease` before the wafer moves to the target. |
| Tower green indicates simulator sequence completion. | The final complete state shows `TowerGreen=true` with FOUP B 5/5. |
| Reset returns the visual to a safe simulator state. | Reset returns to FOUP A loaded, blade retracted, vacuum off, all chamber doors closed, and Z Safe. |
| FOUP A count decreases from 5 to 0. | Captured states show FOUP A 5/5 at startup, 4/5 after W01 pick, and 0/5 while the pipeline drains. |
| FOUP B count increases from 0 to 5. | Captured states show B1 filled after W01 and all B1-B5 filled at completion. |
| Chambers are used as a pipeline. | The state trace records Chamber A/B/C wafer ownership and process state while the five-wafer scheduler drains downstream first. |
| Scheduler drains downstream first. | The timeline only unloads completed chambers and uses the priority C -> FOUP B, B -> C, A -> B, FOUP A -> A. |
| Runtime demo does not auto-close or auto-reset. | The only shutdown calls live in explicit capture-mode startup paths; normal `Run Teaching Demo` leaves the window open at FOUP B 5/5 completed until Reset is pressed. |

## Screenshot Timeline

| Screenshot | What to check visually |
|---|---|
| [00-startup-simulator.png](screenshots/00-startup-simulator.png) | FOUP A starts with W01-W05 waiting. Blade retracted, vacuum off, all chamber doors closed. |
| [01-foup-a-before-pickup.png](screenshots/01-foup-a-before-pickup.png) | Theta target FOUP A Slot A1; preparing to pick W01. |
| [02-blade-holding-wafer-after-pickup.png](screenshots/02-blade-holding-wafer-after-pickup.png) | W01 is now held on the blade; source slot/stage is empty. |
| [03-chamber-a-door-opening.png](screenshots/03-chamber-a-door-opening.png) | Chamber A door opening; blade remains retracted. |
| [04-blade-entering-chamber-a-door-open.png](screenshots/04-blade-entering-chamber-a-door-open.png) | Blade enters Chamber A only while the chamber door is open. |
| [05-wafer-placed-chamber-a-stage.png](screenshots/05-wafer-placed-chamber-a-stage.png) | W01 moved from blade to Chamber A; blade is now empty. |
| [06-blade-retracted-before-chamber-a-door-closes.png](screenshots/06-blade-retracted-before-chamber-a-door-closes.png) | Door close is blocked until the blade is fully retracted. |
| [07-chamber-a-processing-door-closed.png](screenshots/07-chamber-a-processing-door-closed.png) | Chamber A process starts only after wafer is on the stage and door is closed. |
| [08-chamber-a-unload-after-process-complete.png](screenshots/08-chamber-a-unload-after-process-complete.png) | Blade enters Chamber A only after the target path is safe. |
| [09-final-foup-b-5-completed.png](screenshots/09-final-foup-b-5-completed.png) | All 5 wafers completed in FOUP B. Tower green blink enabled. |
| [10-reset-safe-state.png](screenshots/10-reset-safe-state.png) | Reset returns FOUP A to W01-W05 waiting, FOUP B empty, blade retracted, vacuum off, all doors closed. |

## Known Limitations

- This evidence pack is simulator-mode only.
- It does not prove that the new WPF app has been verified on physical equipment.
- Real hardware feedback depends on the local vendor DLL, EtherCAT wiring, E-stop path, and supervised commissioning.
- If the real adapter exposes only commanded state, the UI must label it as commanded or last-known state.
- The approved real-equipment photo is portfolio context, not proof of WPF real-hardware commissioning.

## Generated Files

- `ui-runtime-verification.md`
- `machine-twin-state-trace.json`
- `machine-twin-state-trace.csv`
- `event-log.txt`
- `docs/debug/latest/screenshots/00-startup-simulator.png`
- `docs/debug/latest/screenshots/01-foup-a-before-pickup.png`
- `docs/debug/latest/screenshots/02-blade-holding-wafer-after-pickup.png`
- `docs/debug/latest/screenshots/03-chamber-a-door-opening.png`
- `docs/debug/latest/screenshots/04-blade-entering-chamber-a-door-open.png`
- `docs/debug/latest/screenshots/05-wafer-placed-chamber-a-stage.png`
- `docs/debug/latest/screenshots/06-blade-retracted-before-chamber-a-door-closes.png`
- `docs/debug/latest/screenshots/07-chamber-a-processing-door-closed.png`
- `docs/debug/latest/screenshots/08-chamber-a-unload-after-process-complete.png`
- `docs/debug/latest/screenshots/09-final-foup-b-5-completed.png`
- `docs/debug/latest/screenshots/10-reset-safe-state.png`

## Trace Files

- `machine-twin-state-trace.json`
- `machine-twin-state-trace.csv`
- `event-log.txt`
