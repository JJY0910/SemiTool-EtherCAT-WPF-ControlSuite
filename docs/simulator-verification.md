# Simulator Verification

## Purpose

This checklist verifies that the WPF/MVVM control suite can run its core equipment-control services without real EtherCAT hardware or the vendor DLL.

## What This Verifies

- The solution restores, builds, and tests on a Windows developer PC.
- The simulator starts in a safe state with outputs off.
- Simulator connect, disconnect, input, output, motion, emergency stop, and alarm paths are covered by automated tests.
- Preserved equipment profile values remain loaded from `config/EquipmentProfile.finaltest.json`.
- Application services keep hardware access behind `IEthercatController`.

## What This Does NOT Verify

- Real EtherCAT communication timing.
- Vendor DLL compatibility.
- Physical axis direction, wiring, limit sensors, E-stop wiring, or actuator response.
- Vacuum, cylinder, and door behavior on the actual machine.
- Operator safety on the real tool.

## Commands

```powershell
dotnet restore SemiTool.EtherCAT.WPF.ControlSuite.sln
dotnet build SemiTool.EtherCAT.WPF.ControlSuite.sln --configuration Release --no-restore
dotnet test SemiTool.EtherCAT.WPF.ControlSuite.sln --configuration Release --no-build --no-restore
```

## Simulator Verification Checklist

- [ ] Confirm the app defaults to Simulator mode.
- [ ] Confirm no real hardware auto-connect occurs.
- [ ] Confirm all simulator outputs are off before commands.
- [ ] Connect the simulator manually.
- [ ] Toggle inputs in I/O Monitor.
- [ ] Turn selected outputs on and off in Manual Control.
- [ ] Servo ON in simulator.
- [ ] Home Z and Theta in simulator.
- [ ] Move Z and Theta to small test positions.
- [ ] Run safe simulator-only cylinder and vacuum commands.
- [ ] Trigger a safe simulator timeout/alarm case.
- [ ] Confirm Reset clears active alarms.

## Expected Result

Local build and tests should pass, and simulator-only commands should operate without the vendor DLL or real EtherCAT controller.

## Real Hardware Verification Boundary

Passing simulator verification does not prove physical machine readiness. Real hardware mode requires supervised commissioning with the local vendor DLL, verified wiring, E-stop path, motion limits, and operator approval.
