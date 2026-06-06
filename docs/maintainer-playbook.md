# Maintainer Playbook

## Normal Review Flow

1. Confirm the branch and working tree.
2. Read the changed files before staging.
3. Check whether preserved equipment values are touched.
4. Run build and tests.
5. If Machine Twin visuals changed, regenerate screenshot evidence.
6. Push a focused branch and open a pull request with safety boundaries.

## Required Commands

```powershell
dotnet restore SemiTool.EtherCAT.WPF.ControlSuite.sln
dotnet build SemiTool.EtherCAT.WPF.ControlSuite.sln --configuration Release --no-restore
dotnet test SemiTool.EtherCAT.WPF.ControlSuite.sln --configuration Release --no-build --no-restore
```

## Screenshot Evidence

For README-facing screenshots:

```powershell
dotnet run --project src/SemiTool.Hmi.Wpf/SemiTool.Hmi.Wpf.csproj --configuration Release -- --capture-sequence-assets
```

For full-pipeline QA:

```powershell
dotnet run --project src/SemiTool.Hmi.Wpf/SemiTool.Hmi.Wpf.csproj --configuration Release -- --capture-full-pipeline-qa
```

## Hardware Boundary Checks

- Startup must remain Simulator mode.
- Real Hardware mode must require explicit selection, unlock, and manual Connect.
- No application service should call raw DO/DI numbers.
- No code outside `Ieg3268EthercatController` should load or call the vendor DLL.
- Capture commands must not connect to real EtherCAT hardware.

## Release Notes

Use `CHANGELOG.md` for user-visible changes. Keep wording clear about simulator-only verification unless real hardware was actually tested and documented.

## When Validation Is Blocked

Report:

- command executed
- exact failure
- whether code was built before the blocker
- what remains unverified
- next command to rerun

Do not claim production readiness when validation is blocked.
