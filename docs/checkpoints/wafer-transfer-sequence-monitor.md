# Wafer Transfer Sequence Monitor Checkpoint

## Stable Main Commit

- Main commit: `110fa3d5ccdeccc7cd7851cd083388d1a4be6f2c`
- Scope: Machine Twin / Wafer Transfer Sequence Monitor stable checkpoint
- Date: 2026-05-30

## Recent PR Summary

- PR #1: fixed the transfer-sequence mechanics.
  - Replaced fast snapshot-style motion with readable chamber door, blade, wafer pick/place, retract, door close, and process phases.
  - Fixed the WPF cross-thread command exception.
  - Fixed Z Work and VacuumExhaust simulator output mapping.
  - Removed automatic reset after completion.
  - Kept simulator/capture/designer paths decoupled from hardware DLL loading.
  - Prevented RealHardware status checks from lazy-loading hardware before explicit unlocked Connect.

- PR #2: fixed field HMI terminology and layout.
  - Replaced training/demo-style visible wording with `Run Transfer Sequence`, `Sequence Run`, and `Sequence Complete`.
  - Reframed the screen as `Wafer Transfer Sequence Monitor`.
  - Added the Source / Wafer / Destination operation strip.
  - Renamed safe internal `Teaching*` identifiers toward `WaferTransferSequence`, `MachineTwinSequencePlan`, and transfer-sequence terminology.

- PR #3: preserved visual QA report and evidence.
  - Added the latest visual QA report for the Wafer Transfer Sequence Monitor captures.
  - Preserved regenerated runtime debug evidence and state trace artifacts.
  - Confirmed required wafer transfer states and W01-W05 exactly-once visual/state-trace coverage at reviewed milestones.

## Validation Summary

- Build: success
- Tests: 99 passed / 0 failed
- Capture commands: success
  - `dotnet run --project src\SemiTool.Hmi.Wpf\SemiTool.Hmi.Wpf.csproj --configuration Release -- --capture-sequence-assets`
  - `dotnet run --project src\SemiTool.Hmi.Wpf\SemiTool.Hmi.Wpf.csproj --configuration Release -- --capture-ui-debug-report`
- GitHub Actions: success on main

## Hardware Boundary

- RealHardware adapter behavior was not changed.
- Vendor DLL handling was not changed.
- Preserved theta/detent/axis values were not changed.
- Real hardware I/O mapping semantics were not changed.
- No new real equipment validation is claimed.

## Next Recommended Work

1. Manually review the actual running WPF app window on a Windows/Visual Studio machine.
2. Run the Machine Twin transfer sequence in simulator mode and check operator readability at the real monitor scale.
3. Only open targeted UI-polish work if a concrete operator-readability defect is found, such as unreadable actuator badges, unclear chamber door state, unclear blade state, or insufficient FOUP slot readability.
