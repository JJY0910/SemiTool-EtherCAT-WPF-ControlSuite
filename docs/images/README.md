# Demo Images

Portfolio screenshots in this folder are generated from the WPF app in Simulator mode.

## Current simulator-mode files

- `digital-twin-limited-theta-swing.png` - exists
- `digital-twin-wafer-transfer-robot.png` - exists
- `digital-twin-blade-mechanism.png` - exists
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

## Physical model note

These are simulator-mode generated visuals.

The visual model is based on the wafer transfer robot teaching equipment and the previous CMP HMI scenario. `CMP Cluster` is a simulator scenario name, while the physical model is explained as a limited-swing wafer transfer robot with a telescopic blade/end-effector.

The theta axis is shown as a limited station-to-station swing, not a 360-degree continuous rotation.

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

Do not commit large real-hardware videos, reference spreadsheets, reference documents, or vendor DLLs.
