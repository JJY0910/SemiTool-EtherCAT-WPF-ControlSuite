# Sequence Images

This folder contains GitHub-facing simulator images generated from the WPF application.

## Current files

- `machine-twin-runtime.png` - actual runtime `MainWindow` with the 3D `Machine Twin` tab selected.
- `digital-twin-limited-theta-swing.png` - 3D Machine Twin overview at startup.
- `digital-twin-wafer-transfer-robot.png` - Chamber A transfer state.
- `digital-twin-blade-mechanism.png` - blade/end-effector transfer state.
- `sequence-frame-01.png` - FOUP A pickup target.
- `sequence-frame-02.png` - blade entering Chamber A with the chamber door open.
- `sequence-frame-03.png` - Chamber A processing with the wafer hidden inside the chamber.
- `sequence-frame-04.png` - final FOUP B completion state.
- `dashboard.png`
- `manual-control.png`
- `io-monitor.png`
- `auto-sequence.png`
- `wafer-flow.png`
- `alarm-log.png`
- `settings.png`
- `real-equipment-context-top-view.jpg` - user-provided real equipment context reference.

## Regeneration command

```powershell
dotnet run --project src/SemiTool.Hmi.Wpf/SemiTool.Hmi.Wpf.csproj --configuration Release -- --capture-sequence-assets
```

The capture path uses WPF `RenderTargetBitmap` against the actual views. It stays in Simulator mode, does not load `IEG3268_Dll.dll`, and does not connect to real EtherCAT hardware.

If Windows App Control blocks generated Release DLLs with `0x800711C7`, rerun with `-p:Deterministic=false` before `--`.

## Verification evidence

Runtime and full-pipeline evidence is kept under:

- `docs/debug/latest/runtime-verification/`
- `docs/debug/latest/screenshots/`
- `docs/debug/latest/full-pipeline/`

## Privacy rule

Do not add real equipment photos or videos that expose private school, customer, or machine details unless they are explicitly approved for public repository use.

Do not commit vendor DLLs, large real-hardware videos, private reference spreadsheets, or private reference documents.
