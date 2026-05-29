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

## Trace Files

- `machine-twin-state-trace.json`
- `machine-twin-state-trace.csv`
- `event-log.txt`
