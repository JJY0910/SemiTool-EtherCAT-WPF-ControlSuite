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
- Normal runtime `Run Simulator Demo` remains open after completion; only explicit capture modes call application shutdown.

## Runtime Integration Check

- MainWindow first tab is `Machine Twin`.
- MainWindow uses `<views:MachineTwinView DataContext="{Binding MachineTwin}" />`.
- MainViewModel exposes `MachineTwinViewModel` through the `MachineTwin` property.
- `Run Simulator Demo` is a command on the actual `MachineTwinView` runtime screen.
- `00-startup-simulator.png` is captured from the actual `MainWindow`, so it shows the selected `Machine Twin` tab.
- The remaining screenshots are captured from the same `MachineTwinView` and `MachineTwinViewModel` used by the running app.

## Captured Steps

| Step | State | Station | FOUP A | FOUP B | Chambers | Z | Blade | Vacuum | Screenshot |
|---:|---|---|---:|---:|---|---|---|---|---|
| 0 | Startup Simulator | FOUP A | 5/5 | 0/5 | Chamber A:Empty::PreClean_Default:-:0s:0%<br>Chamber B:Empty::CMP_Main:-:0s:0%<br>Chamber C:Empty::PostClean_Dry:-:0s:0% | Z Safe | Retracted | Off | [00-startup-simulator.png](screenshots/00-startup-simulator.png) |
| 1 | FOUP A 5 Wafers | FOUP A | 5/5 | 0/5 | Chamber A:Empty::PreClean_Default:-:0s:0%<br>Chamber B:Empty::CMP_Main:-:0s:0%<br>Chamber C:Empty::PostClean_Dry:-:0s:0% | Z Safe | Retracted | Off | [01-foup-a-5-wafers.png](screenshots/01-foup-a-5-wafers.png) |
| 2 | W01 Pick A1 | FOUP A | 4/5 | 0/5 | Chamber A:Empty::PreClean_Default:-:0s:0%<br>Chamber B:Empty::CMP_Main:-:0s:0%<br>Chamber C:Empty::PostClean_Dry:-:0s:0% | Z Work | Extended | Off | [02-w01-pick-a1.png](screenshots/02-w01-pick-a1.png) |
| 3 | W01 On Blade | FOUP A | 4/5 | 0/5 | Chamber A:Empty::PreClean_Default:-:0s:0%<br>Chamber B:Empty::CMP_Main:-:0s:0%<br>Chamber C:Empty::PostClean_Dry:-:0s:0% | Z Work | Extended | Suction | [03-w01-on-blade.png](screenshots/03-w01-on-blade.png) |
| 4 | W01 Chamber A Processing | Chamber A | 4/5 | 0/5 | Chamber A:Processing:W01:PreClean_Default:Chem Clean:8s:25%<br>Chamber B:Empty::CMP_Main:-:0s:0%<br>Chamber C:Empty::PostClean_Dry:-:0s:0% | Z Safe | Retracted | Off | [04-w01-chamber-a-processing.png](screenshots/04-w01-chamber-a-processing.png) |
| 5 | W02 Feeds While W01 Moves To B | Chamber A | 3/5 | 0/5 | Chamber A:Processing:W02:PreClean_Default:Chem Clean:8s:20%<br>Chamber B:Processing:W01:CMP_Main:Bulk Polish:10s:15%<br>Chamber C:Empty::PostClean_Dry:-:0s:0% | Z Work | Extended | Exhaust | [05-w02-enters-chamber-a-while-w01-moves-to-b.png](screenshots/05-w02-enters-chamber-a-while-w01-moves-to-b.png) |
| 6 | Three Chambers Occupied | Chamber B (CMP) | 2/5 | 0/5 | Chamber A:Processing:W03:PreClean_Default:Chem Clean:8s:15%<br>Chamber B:Processing:W02:CMP_Main:Bulk Polish:10s:55%<br>Chamber C:Processing:W01:PostClean_Dry:Spin Dry:8s:35% | Z Safe | Retracted | Off | [06-pipeline-three-chambers-occupied.png](screenshots/06-pipeline-three-chambers-occupied.png) |
| 7 | W01 Chamber C Complete | Chamber C | 2/5 | 0/5 | Chamber A:Processing:W03:PreClean_Default:Chem Clean:8s:45%<br>Chamber B:Processing:W02:CMP_Main:Bulk Polish:10s:70%<br>Chamber C:Completed:W01:PostClean_Dry:Spin Dry complete:0s:100% | Z Safe | Retracted | Off | [07-w01-chamber-c-complete.png](screenshots/07-w01-chamber-c-complete.png) |
| 8 | W01 Placed FOUP B B1 | FOUP B | 1/5 | 1/5 | Chamber A:Processing:W04:PreClean_Default:Chem Clean:8s:10%<br>Chamber B:Processing:W03:CMP_Main:Bulk Polish:10s:35%<br>Chamber C:Processing:W02:PostClean_Dry:Spin Dry:8s:20% | Z Work | Extended | Exhaust | [08-w01-placed-foup-b-b1.png](screenshots/08-w01-placed-foup-b-b1.png) |
| 9 | FOUP A Empty Pipeline Finishing | Chamber B (CMP) | 0/5 | 3/5 | Chamber A:Empty::PreClean_Default:-:0s:0%<br>Chamber B:Completed:W05:CMP_Main:Bulk Polish complete:0s:100%<br>Chamber C:Processing:W04:PostClean_Dry:Spin Dry:8s:70% | Z Safe | Retracted | Off | [09-foup-a-empty-pipeline-finishing.png](screenshots/09-foup-a-empty-pipeline-finishing.png) |
| 10 | FOUP B 5 Wafers Complete | FOUP B | 0/5 | 5/5 | Chamber A:Empty::PreClean_Default:-:0s:0%<br>Chamber B:Empty::CMP_Main:-:0s:0%<br>Chamber C:Empty::PostClean_Dry:-:0s:0% | Z Safe | Retracted | Off | [10-foup-b-5-wafers-complete.png](screenshots/10-foup-b-5-wafers-complete.png) |
| 11 | Reset Safe State | FOUP A | 5/5 | 0/5 | Chamber A:Empty::PreClean_Default:-:0s:0%<br>Chamber B:Empty::CMP_Main:-:0s:0%<br>Chamber C:Empty::PostClean_Dry:-:0s:0% | Z Safe | Retracted | Off | [11-reset-safe-state.png](screenshots/11-reset-safe-state.png) |

## Expected vs Actual Movement

| Expected simulator movement | Evidence in this report |
|---|---|
| Machine Twin starts in Simulator mode and does not connect to real hardware. | Step 0 shows `IsSimulatorMode=true` and `IsRealHardwareMode=false`; `IsConnected` refers to the simulator controller connection, not real equipment. |
| FOUP A starts with five wafers. | Steps 0 and 1 show `FoupACount=5` and `FoupBCount=0`. |
| Theta target follows the limited station arc instead of a 360-degree dial. | The trace records station-to-station `ThetaTargetName` changes plus preserved encoder values. |
| Z moves from Safe to Work only during pick/place visualization. | Pick/place steps show `ZState=Z Work`; processing and reset states return to `Z Safe`. |
| Cylinder forward extends the telescopic blade. | Steps with `IsCylinderForward=true` also show `IsBladeExtended=true`. |
| Vacuum suction attaches the wafer to the blade. | Step 3 shows `VacuumState=Suction`, `IsVacuumOn=true`, and `IsWaferOnBlade=true`. |
| Vacuum exhaust/release places the wafer into the chamber or FOUP. | Placement steps turn vacuum off and move the wafer flag to the target location. |
| Tower green indicates simulator sequence completion. | Step 10 shows `TowerGreen=true`. |
| Reset returns the visual to a safe simulator state. | Step 11 returns to FOUP A, blade retracted, vacuum off, and Z Safe. |
| FOUP A count decreases from 5 to 0. | Captured states show FOUP A 5/5 at startup, 4/5 after W01 pick, and 0/5 while the pipeline drains. |
| FOUP B count increases from 0 to 5. | Captured states show B1 filled after W01 and all B1-B5 filled at completion. |
| Chambers are used as a pipeline. | Step 6 shows Chamber A, Chamber B, and Chamber C occupied simultaneously. |
| Scheduler drains downstream first. | Step 7 shows Chamber C complete and ready for the highest-priority Chamber C -> FOUP B transfer. |
| Runtime demo does not auto-close. | The only shutdown calls live in explicit capture-mode startup paths; normal `Run Simulator Demo` leaves the window open. |

## Screenshot Timeline

| Screenshot | What to check visually |
|---|---|
| [00-startup-simulator.png](screenshots/00-startup-simulator.png) | Pipeline ready: FOUP A 5 wafers, FOUP B empty. |
| [01-foup-a-5-wafers.png](screenshots/01-foup-a-5-wafers.png) | FOUP A slots A1-A5 are loaded. FOUP B is empty. |
| [02-w01-pick-a1.png](screenshots/02-w01-pick-a1.png) | Transfer W01: FOUP A Slot A1 -> Chamber A started. Z Work, blade extended. |
| [03-w01-on-blade.png](screenshots/03-w01-on-blade.png) | Vacuum suction ON: W01 is on the blade. |
| [04-w01-chamber-a-processing.png](screenshots/04-w01-chamber-a-processing.png) | W01 placed in Chamber A. PreClean_Default started. |
| [05-w02-enters-chamber-a-while-w01-moves-to-b.png](screenshots/05-w02-enters-chamber-a-while-w01-moves-to-b.png) | Scheduler fed W02 into Chamber A after W01 moved to Chamber B. |
| [06-pipeline-three-chambers-occupied.png](screenshots/06-pipeline-three-chambers-occupied.png) | Pipeline state: Chamber A W03, Chamber B W02, Chamber C W01. |
| [07-w01-chamber-c-complete.png](screenshots/07-w01-chamber-c-complete.png) | W01 Chamber C process complete. Scheduler priority selects Chamber C -> FOUP B. |
| [08-w01-placed-foup-b-b1.png](screenshots/08-w01-placed-foup-b-b1.png) | W01 placed into FOUP B Slot B1. FOUP B count is now 1/5. |
| [09-foup-a-empty-pipeline-finishing.png](screenshots/09-foup-a-empty-pipeline-finishing.png) | FOUP A is empty. Pipeline is draining remaining wafers toward FOUP B. |
| [10-foup-b-5-wafers-complete.png](screenshots/10-foup-b-5-wafers-complete.png) | All 5 wafers completed in FOUP B. Tower green blink enabled. |
| [11-reset-safe-state.png](screenshots/11-reset-safe-state.png) | Reset returns simulator to FOUP A loaded, FOUP B empty, blade retracted, vacuum off. |

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
- `docs/debug/latest/screenshots/01-foup-a-5-wafers.png`
- `docs/debug/latest/screenshots/02-w01-pick-a1.png`
- `docs/debug/latest/screenshots/03-w01-on-blade.png`
- `docs/debug/latest/screenshots/04-w01-chamber-a-processing.png`
- `docs/debug/latest/screenshots/05-w02-enters-chamber-a-while-w01-moves-to-b.png`
- `docs/debug/latest/screenshots/06-pipeline-three-chambers-occupied.png`
- `docs/debug/latest/screenshots/07-w01-chamber-c-complete.png`
- `docs/debug/latest/screenshots/08-w01-placed-foup-b-b1.png`
- `docs/debug/latest/screenshots/09-foup-a-empty-pipeline-finishing.png`
- `docs/debug/latest/screenshots/10-foup-b-5-wafers-complete.png`
- `docs/debug/latest/screenshots/11-reset-safe-state.png`

## Trace Files

- `machine-twin-state-trace.json`
- `machine-twin-state-trace.csv`
- `event-log.txt`
