# Demo Assets

## How Assets Were Generated

The portfolio images were generated from the WPF application in simulator-only capture mode.

```powershell
dotnet run --project src/SemiTool.Hmi.Wpf/SemiTool.Hmi.Wpf.csproj -- --capture-demo-assets
```

The capture mode uses WPF `RenderTargetBitmap` and existing `UserControl` views. It does not select Real Hardware mode, does not load the vendor DLL, and does not connect to real equipment.

## Generated Simulator-Mode Assets

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

## Real Equipment Context Asset

- `docs/images/real-equipment-context-top-view.jpg`

This photo is a real-equipment context reference for the original WinForms EtherCAT control experience. It supports the portfolio explanation by showing the physical three-chamber layout and central transfer mechanism, but it does not claim that the new WPF app has completed real-hardware verification.

## How To Regenerate

1. Open Windows Native PowerShell.
2. Run from the repository root.
3. Execute the capture command above.
4. Validate dimensions and file sizes before committing.

## Privacy Rule

Do not add real equipment photos or videos that expose private school, customer, or machine details unless they are approved for public portfolio use.

Real hardware video should only be added after supervised commissioning.
