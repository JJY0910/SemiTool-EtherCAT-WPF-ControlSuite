# AGENTS.md

## Project Rules

This is a clean WPF/MVVM semiconductor equipment-control project rebuilt from a legacy WinForms EtherCAT project. Do not convert old WinForms forms in place.

## Preserved Values

Do not change preserved hardware values unless a newer `EquipmentProfile.finaltest.json` explicitly requires it.

Protected values:

- DO0-DO15 output map
- DI0-DI5 and DI12-DI13 input map
- Home, FOUP A/B, Chamber A/B/C robot poses
- FOUP slot Z safe/work values
- Motion, door, cylinder, vacuum, polling, and auto tick timing values
- Auto scheduler priority

## Forbidden In Application Logic

- `Thread.Sleep`
- raw `DigitalOutput` or `DigitalInput` integer calls
- direct raw DO/DI channel use such as `WriteDigitalOutputAsync(12, ...)`
- direct vendor DLL usage outside `Ieg3268EthercatController`
- auto-connect, auto-run, auto-motion, or output activation on startup

Use named I/O points:

```csharp
await ethercat.WriteDigitalOutputAsync(IoPoint.CylinderForward, true, ct);
```

## Safety Defaults

- Startup mode is Simulator.
- All outputs must be off on startup.
- Real Hardware mode requires explicit mode selection, hardware unlock, and manual Connect.
- Manual commands are blocked during Auto.
- Auto Start is blocked if disconnected or not homed.

## Build and Test

```powershell
dotnet restore SemiTool.EtherCAT.WPF.ControlSuite.sln
dotnet build SemiTool.EtherCAT.WPF.ControlSuite.sln
dotnet test SemiTool.EtherCAT.WPF.ControlSuite.sln --no-build --no-restore
```

## Git Hygiene

Do not commit:

- vendor DLLs
- `bin/`, `obj/`, `.vs/`
- `.exe`, `.pdb`
- private local settings
- migration input ZIPs or extracted legacy files

## Commenting and Maintainability Rules

- Comment why a block exists, not just what each statement does.
- Add explanatory comments around hardware, safety, DLL loading, reflection, sequence, timeout, and interlock logic.
- Use XML documentation for important public APIs and adapter boundaries.
- Keep comments accurate and avoid misleading claims about real hardware verification.
- Do not change behavior just to add comments or formatting.
- Do not change preserved equipment values.

## XAML Designer Preview Rules

- Any XAML UI change must keep the Visual Studio Designer preview meaningful.
- `MainWindow.xaml` should continue to show the `Machine Twin` tab as the first/default design-time view.
- `MachineTwinView.xaml` should continue to render FOUP slots, chamber states, limited theta swing, blade, vacuum, tower lamp, and event log sample rows in the Designer.
- Design-time data must stay under `src/SemiTool.Hmi.Wpf/DesignTime` and must not connect to real hardware or load vendor DLLs.
- Runtime `DataContext` creation must remain in the normal MVVM startup path; `d:DataContext` is for Designer preview only.
