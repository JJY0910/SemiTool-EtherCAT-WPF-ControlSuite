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

## Runtime Integration Check

- MainWindow first tab is `Machine Twin`.
- MainWindow uses `<views:MachineTwinView DataContext="{Binding MachineTwin}" />`.
- MainViewModel exposes `MachineTwinViewModel` through the `MachineTwin` property.
- `Run Simulator Demo` is a command on the actual `MachineTwinView` runtime screen.
- `00-startup-simulator.png` is captured from the actual `MainWindow`, so it shows the selected `Machine Twin` tab.
- The remaining screenshots are captured from the same `MachineTwinView` and `MachineTwinViewModel` used by the running app.

## Captured Steps

| Step | State | Station | Z | Blade | Vacuum | Wafer | Screenshot |
|---:|---|---|---|---|---|---|---|
| 0 | Startup Simulator | FOUP A | Z Safe | Retracted | OFF | Simulator startup / safe state | [00-startup-simulator.png](screenshots/00-startup-simulator.png) |
| 1 | Initial FOUP A Slot 1 | FOUP A | Z Safe | Retracted | OFF | FOUP A Slot 1 contains wafer | [01-initial-foup-a-slot1.png](screenshots/01-initial-foup-a-slot1.png) |
| 2 | Theta To FOUP A | FOUP A | Z Safe | Retracted | OFF | Theta target FOUP A | [02-theta-to-foup-a.png](screenshots/02-theta-to-foup-a.png) |
| 3 | Z Work / Blade Extend | FOUP A | Z Work | Extended | OFF | Z Work and blade extended into FOUP A | [03-z-work-blade-extend.png](screenshots/03-z-work-blade-extend.png) |
| 4 | Vacuum Suction / Wafer On Blade | FOUP A | Z Work | Extended | ON | On blade | [04-vacuum-suction-wafer-on-blade.png](screenshots/04-vacuum-suction-wafer-on-blade.png) |
| 5 | Transfer To Chamber A | Chamber A | Z Safe | Retracted | ON | On blade | [05-transfer-to-chamber-a.png](screenshots/05-transfer-to-chamber-a.png) |
| 6 | Place Chamber A | Chamber A | Z Work | Extended | OFF | Release wafer into Chamber A / PreClean starts | [06-place-chamber-a.png](screenshots/06-place-chamber-a.png) |
| 7 | Transfer Chamber A To B | Chamber B (CMP) | Z Work | Extended | OFF | Move wafer to Chamber B CMP_Main | [07-transfer-chamber-a-to-b.png](screenshots/07-transfer-chamber-a-to-b.png) |
| 8 | Transfer Chamber B To C | Chamber C | Z Work | Extended | OFF | Move wafer to Chamber C PostClean_Dry | [08-transfer-chamber-b-to-c.png](screenshots/08-transfer-chamber-b-to-c.png) |
| 9 | Transfer Chamber C To FOUP B | FOUP B | Z Work | Extended | OFF | Place wafer into FOUP B Slot 1 | [09-transfer-chamber-c-to-foup-b.png](screenshots/09-transfer-chamber-c-to-foup-b.png) |
| 10 | Process Complete Green Blink | FOUP B | Z Safe | Retracted | OFF | Overall simulator flow complete | [10-process-complete-green-blink.png](screenshots/10-process-complete-green-blink.png) |
| 11 | Reset Safe State | FOUP A | Z Safe | Retracted | OFF | Reset to safe simulator state | [11-reset-safe-state.png](screenshots/11-reset-safe-state.png) |

## Expected vs Actual Movement

| Expected simulator movement | Evidence in this report |
|---|---|
| Machine Twin starts in Simulator mode and does not connect to real hardware. | Step 0 shows `IsSimulatorMode=true` and `IsRealHardwareMode=false`; `IsConnected` refers to the simulator controller connection, not real equipment. |
| FOUP A Slot 1 starts with a wafer. | Step 1 keeps `IsWaferInFoupA1=true` and no wafer on the blade. |
| Theta target follows the limited station arc instead of a 360-degree dial. | Steps 2, 5, 7, 8, and 9 show station-to-station `ThetaTargetName` changes plus preserved encoder values. |
| Z moves from Safe to Work only during pick/place visualization. | Steps 3, 4, 6, 7, 8, and 9 show `ZState=Z Work`; reset returns to `Z Safe`. |
| Cylinder forward extends the telescopic blade. | Steps with `IsCylinderForward=true` also show `IsBladeExtended=true`. |
| Vacuum suction attaches the wafer to the blade. | Step 4 shows `IsVacuumOn=true` and `IsWaferOnBlade=true`. |
| Vacuum exhaust/release places the wafer into the chamber or FOUP. | Placement steps turn vacuum off and move the wafer flag to the target location. |
| Tower green indicates simulator sequence completion. | Step 10 shows `TowerGreen=true`. |
| Reset returns the visual to a safe simulator state. | Step 11 returns to FOUP A, blade retracted, vacuum off, and Z Safe. |

## Screenshot Timeline

| Screenshot | What to check visually |
|---|---|
| [00-startup-simulator.png](screenshots/00-startup-simulator.png) | Startup simulator state. No real hardware connected. |
| [01-initial-foup-a-slot1.png](screenshots/01-initial-foup-a-slot1.png) | Wafer A01 is ready in FOUP A Slot 1. |
| [02-theta-to-foup-a.png](screenshots/02-theta-to-foup-a.png) | Limited theta swing targets FOUP A. |
| [03-z-work-blade-extend.png](screenshots/03-z-work-blade-extend.png) | CylinderForward extends the telescopic blade. |
| [04-vacuum-suction-wafer-on-blade.png](screenshots/04-vacuum-suction-wafer-on-blade.png) | VacuumSuction holds wafer A01 on the blade. |
| [05-transfer-to-chamber-a.png](screenshots/05-transfer-to-chamber-a.png) | Blade retracts and theta swings to Chamber A. |
| [06-place-chamber-a.png](screenshots/06-place-chamber-a.png) | VacuumExhaust releases wafer into Chamber A. |
| [07-transfer-chamber-a-to-b.png](screenshots/07-transfer-chamber-a-to-b.png) | Chamber B CMP_Main process starts. |
| [08-transfer-chamber-b-to-c.png](screenshots/08-transfer-chamber-b-to-c.png) | Chamber C PostClean_Dry process starts. |
| [09-transfer-chamber-c-to-foup-b.png](screenshots/09-transfer-chamber-c-to-foup-b.png) | Wafer A01 is placed into FOUP B Slot 1. |
| [10-process-complete-green-blink.png](screenshots/10-process-complete-green-blink.png) | Tower green indicates simulator flow complete. |
| [11-reset-safe-state.png](screenshots/11-reset-safe-state.png) | Reset returns the simulator display to a safe state. |

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
- `docs/debug/latest/screenshots/01-initial-foup-a-slot1.png`
- `docs/debug/latest/screenshots/02-theta-to-foup-a.png`
- `docs/debug/latest/screenshots/03-z-work-blade-extend.png`
- `docs/debug/latest/screenshots/04-vacuum-suction-wafer-on-blade.png`
- `docs/debug/latest/screenshots/05-transfer-to-chamber-a.png`
- `docs/debug/latest/screenshots/06-place-chamber-a.png`
- `docs/debug/latest/screenshots/07-transfer-chamber-a-to-b.png`
- `docs/debug/latest/screenshots/08-transfer-chamber-b-to-c.png`
- `docs/debug/latest/screenshots/09-transfer-chamber-c-to-foup-b.png`
- `docs/debug/latest/screenshots/10-process-complete-green-blink.png`
- `docs/debug/latest/screenshots/11-reset-safe-state.png`

## Trace Files

- `machine-twin-state-trace.json`
- `machine-twin-state-trace.csv`
- `event-log.txt`
