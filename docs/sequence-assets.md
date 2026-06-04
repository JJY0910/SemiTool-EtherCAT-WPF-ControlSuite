# Sequence Asset Generation

## Purpose

The images in `docs/images` are generated from the WPF application in simulator-only capture mode. They are used by the GitHub README and by visual review notes.

```powershell
dotnet run --project src/SemiTool.Hmi.Wpf/SemiTool.Hmi.Wpf.csproj --configuration Release -- --capture-sequence-assets
```

If Windows App Control blocks generated Release DLLs with `0x800711C7`, rerun with `-p:Deterministic=false` before `--`.

## Generated assets

- `docs/images/machine-twin-runtime.png`
- `docs/images/digital-twin-limited-theta-swing.png`
- `docs/images/digital-twin-wafer-transfer-robot.png`
- `docs/images/digital-twin-blade-mechanism.png`
- `docs/images/sequence-frame-01.png`
- `docs/images/sequence-frame-02.png`
- `docs/images/sequence-frame-03.png`
- `docs/images/sequence-frame-04.png`
- `docs/images/dashboard.png`
- `docs/images/manual-control.png`
- `docs/images/io-monitor.png`
- `docs/images/auto-sequence.png`
- `docs/images/wafer-flow.png`
- `docs/images/alarm-log.png`
- `docs/images/settings.png`

## Boundary

These assets are simulator-mode WPF render captures. They do not claim that the WPF implementation has completed real-hardware verification.

Real hardware media should only be added after supervised commissioning and explicit approval for public repository use.

## Related evidence

```powershell
dotnet run --project src/SemiTool.Hmi.Wpf/SemiTool.Hmi.Wpf.csproj --configuration Release -- --capture-ui-debug-report
dotnet run --project src/SemiTool.Hmi.Wpf/SemiTool.Hmi.Wpf.csproj --configuration Release -- --capture-full-pipeline-qa
```

Outputs:

- `docs/debug/latest/ui-runtime-verification.md`
- `docs/debug/latest/machine-twin-state-trace.json`
- `docs/debug/latest/machine-twin-state-trace.csv`
- `docs/debug/latest/event-log.txt`
- `docs/debug/latest/screenshots/*.png`
- `docs/debug/latest/full-pipeline/*.md`
- `docs/debug/latest/full-pipeline/screenshots/*.png`
