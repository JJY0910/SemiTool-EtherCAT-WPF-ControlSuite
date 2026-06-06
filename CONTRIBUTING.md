# Contributing

Thank you for helping improve SemiTool EtherCAT WPF Control Suite. This project is safety-sensitive because it contains a real-hardware adapter path, so small, well-verified changes are preferred.

## Scope Rules

- Keep preserved equipment values unchanged unless a newer approved `config/EquipmentProfile.finaltest.json` requires the change.
- Do not add auto-connect, auto-run, auto-home, auto-motion, or output activation on startup.
- Do not use raw DO/DI integer channels in application logic. Use named `IoPoint` values.
- Do not load the vendor DLL outside `Ieg3268EthercatController`.
- Do not commit vendor DLLs, build output, local settings, logs, or migration input archives.

## Local Setup

```powershell
dotnet restore SemiTool.EtherCAT.WPF.ControlSuite.sln
dotnet build SemiTool.EtherCAT.WPF.ControlSuite.sln --configuration Release --no-restore
dotnet test SemiTool.EtherCAT.WPF.ControlSuite.sln --configuration Release --no-build --no-restore
```

Use Simulator mode for normal development. Real Hardware mode should be exercised only on a supervised equipment PC with the commissioning checklist.

## Pull Requests

Please include:

- the reason for the change
- changed files and the safety boundary
- build/test commands and results
- whether simulator screenshots were regenerated
- any real-equipment limitation or follow-up

Keep PRs focused. Documentation-only updates should not modify application behavior.

## UI Evidence

When changing the Machine Twin view or transfer sequence, regenerate at least one of:

```powershell
dotnet run --project src/SemiTool.Hmi.Wpf/SemiTool.Hmi.Wpf.csproj --configuration Release -- --capture-sequence-assets
dotnet run --project src/SemiTool.Hmi.Wpf/SemiTool.Hmi.Wpf.csproj --configuration Release -- --capture-full-pipeline-qa
```

Do not describe simulator captures as real-hardware verification.
