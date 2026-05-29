# Visual Studio Designer Preview

## Purpose

This project includes design-time sample data so the WPF HMI can be reviewed in Visual Studio Designer before the app is run.

The designer preview is meant for layout, readability, and portfolio review. It is not a simulator execution trace and it is not real-hardware verification.

## Files To Open

Open these files in Visual Studio:

```text
src/SemiTool.Hmi.Wpf/MainWindow.xaml
src/SemiTool.Hmi.Wpf/Views/MachineTwinView.xaml
```

`MainWindow.xaml` should show the full HMI shell with `Machine Twin` selected as the first/default tab.

`MachineTwinView.xaml` should show the Machine Twin view directly.

## What Should Be Visible

The designer preview should show:

- Real equipment context reference photo panel.
- FOUP A 5-slot cassette with W01-W05 sample wafers.
- FOUP B 5-slot cassette with sample completed/empty states.
- Chamber A processing sample wafer W01 with `PreClean_Default`.
- Chamber B sample `CMP_Main` completed state.
- Chamber C sample `PostClean_Dry` empty state.
- Limited theta swing arc with station detents.
- Telescopic blade extended toward a station.
- Wafer on blade with vacuum suction on.
- Z Work sample state.
- Cylinder forward / blade extended indicator.
- Tower green indicator.
- Event log sample rows.

## Design-Time Data

Design-time data lives under:

```text
src/SemiTool.Hmi.Wpf/DesignTime
```

The key classes are:

- `DesignMainViewModel`
- `DesignMachineTwinViewModel`
- `DesignMachineTwinData`

These classes are referenced through `d:DataContext`, so they are used only by the Visual Studio Designer.

Runtime startup still creates `MainViewModel` in `App.xaml.cs`, and runtime Machine Twin state still comes from `MachineTwinViewModel`.

## Safety Boundary

Designer preview:

- Does not connect to EtherCAT.
- Does not load `IEG3268_Dll.dll`.
- Does not require vendor DLLs.
- Does not connect to real hardware.
- Does not actuate outputs.
- Does not prove that the new WPF app has been verified on real hardware.

The original WinForms project controlled real EtherCAT hardware. The new WPF app is prepared for supervised real-hardware verification, but that verification is still separate work.

## Runtime Motion

The designer is static. To verify motion, run:

```powershell
dotnet run --project src/SemiTool.Hmi.Wpf/SemiTool.Hmi.Wpf.csproj --configuration Release
```

Then open the `Machine Twin` tab and click `Run Simulator Demo`.

For repeatable evidence generation, run:

```powershell
dotnet run --project src/SemiTool.Hmi.Wpf/SemiTool.Hmi.Wpf.csproj --configuration Release -- --capture-ui-debug-report
```

The generated report is:

```text
docs/debug/latest/ui-runtime-verification.md
```

## Troubleshooting

If the designer does not load:

- Build the solution once in Visual Studio.
- Confirm `src/SemiTool.Hmi.Wpf/Styles/EquipmentTheme.xaml` is available.
- Confirm `docs/images/real-equipment-context-top-view.jpg` exists.
- Confirm `src/SemiTool.Hmi.Wpf/Assets/real-equipment-context-top-view.jpg` appears as a linked content item in the project.
- Reopen `MainWindow.xaml` or `MachineTwinView.xaml`.

If the image does not show, the rest of the Machine Twin preview should still render with sample FOUP, chamber, robot, and event log state.
