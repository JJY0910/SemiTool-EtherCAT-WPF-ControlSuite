# Demo Images

Portfolio screenshots in this folder are generated from the WPF app in Simulator mode.

## Current simulator-mode files

- `dashboard.png` - exists
- `manual-control.png` - exists
- `io-monitor.png` - exists
- `auto-sequence.png` - exists
- `wafer-flow.png` - exists
- `alarm-log.png` - exists
- `settings.png` - exists
- `simulator-demo-frame-01.png` - exists
- `simulator-demo-frame-02.png` - exists
- `simulator-demo-frame-03.png` - exists
- `simulator-demo-frame-04.png` - exists

## Current real-equipment context file

- `real-equipment-context-top-view.jpg` - exists

This is a top-view reference photo used to explain the physical equipment context behind the original WinForms EtherCAT control project. It is not a claim that the new WPF implementation has already been verified on real hardware.

## Optional media

- `real-hardware-short-test.mp4`
- `simulator-demo.gif`

Do not commit huge videos directly. Use compressed GIFs, GitHub Releases, or an external video link if needed.

## Regeneration command

```powershell
dotnet run --project src/SemiTool.Hmi.Wpf/SemiTool.Hmi.Wpf.csproj -- --capture-demo-assets
```

## Privacy rule

Do not add real equipment photos or videos that expose private school, customer, or machine details unless they are approved for public portfolio use.
