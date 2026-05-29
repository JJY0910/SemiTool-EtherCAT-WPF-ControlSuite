# Simulator Images

Portfolio screenshots in this folder are generated from the WPF app in Simulator mode.

## Current simulator-mode files

- `digital-twin-limited-theta-swing.png` - exists
- `digital-twin-wafer-transfer-robot.png` - exists
- `digital-twin-blade-mechanism.png` - exists
- `machine-twin-runtime.png` - exists, captured from the actual runtime `MachineTwinView`
- `real-equipment-context-top-view.jpg` - exists, approved user-provided public context photo
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

These are simulator-mode generated visuals, except `real-equipment-context-top-view.jpg`, which is a user-approved real equipment context reference photo.

The visual model is based on the wafer transfer robot sequence monitor and the previous CMP HMI scenario. `CMP Cluster` is a simulator scenario name, while the physical model is explained as a limited-swing wafer transfer robot with a telescopic blade/end-effector.

The theta axis is shown as a limited station-to-station swing, not a 360-degree continuous rotation.

`machine-twin-runtime.png` and `docs/debug/latest/screenshots/*.png` are rendered from the actual WPF `MachineTwinView`, not from a disconnected mockup.

## Optional media

- `real-hardware-short-test.mp4`
- `simulator-demo.gif`

Do not commit huge videos directly. Use compressed GIFs, GitHub Releases, or an external video link if needed.

## Regeneration command

```powershell
dotnet run --project src/SemiTool.Hmi.Wpf/SemiTool.Hmi.Wpf.csproj -- --capture-demo-assets
```

Runtime UI evidence pack:

```powershell
dotnet run --project src/SemiTool.Hmi.Wpf/SemiTool.Hmi.Wpf.csproj --configuration Release -- --capture-ui-debug-report
```

## Privacy rule

Do not add real equipment photos or videos that expose private school, customer, or machine details unless they are approved for public portfolio use.

Do not commit large real-hardware videos, reference spreadsheets, reference documents, or vendor DLLs.
