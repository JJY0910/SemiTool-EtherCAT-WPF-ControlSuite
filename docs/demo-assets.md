# Demo Assets

## How Assets Were Generated

The portfolio images were generated from the WPF application in simulator-only capture mode.

```powershell
dotnet run --project src/SemiTool.Hmi.Wpf/SemiTool.Hmi.Wpf.csproj -- --capture-demo-assets
```

The capture mode uses WPF `RenderTargetBitmap` and existing `UserControl` views. It does not select Real Hardware mode, does not load the vendor DLL, and does not connect to real equipment.

## Generated Simulator-Mode Assets

- `docs/images/digital-twin-limited-theta-swing.png`
- `docs/images/digital-twin-wafer-transfer-robot.png`
- `docs/images/digital-twin-blade-mechanism.png`
- `docs/images/dashboard.png`
- `docs/images/manual-control.png`
- `docs/images/io-monitor.png`
- `docs/images/auto-sequence.png`
- `docs/images/wafer-flow.png`
- `docs/images/alarm-log.png`
- `docs/images/settings.png`
- `docs/images/simulator-demo-frame-01.png`
- `docs/images/simulator-demo-frame-02.png`
- `docs/images/simulator-demo-frame-03.png`
- `docs/images/simulator-demo-frame-04.png`

## Physical Model Context

The Digital Twin images are generated simulator-mode abstractions. They represent a wafer transfer robot teaching setup with:

- a fixed aluminum-like base
- a central limited-swing theta base
- a two-stage/telescopic blade/end-effector
- Z Safe/Work movement
- cylinder extend/retract
- vacuum suction/exhaust
- FOUP A, Chamber A, Chamber B, Chamber C, and FOUP B stations

The reference photo is understanding-only material and is not committed to the public repository. The simulator visuals do not claim that the new WPF app has completed real-hardware verification.

## How To Regenerate

1. Open Windows Native PowerShell.
2. Run from the repository root.
3. Execute the capture command above.
4. Validate dimensions and file sizes before committing.

## Privacy Rule

Do not add real equipment photos or videos that expose private school, customer, or machine details unless they are approved for public portfolio use.

Real hardware video should only be added after supervised commissioning.
