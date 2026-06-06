# SemiTool EtherCAT WPF Control Suite

[![.NET CI](https://github.com/JJY0910/SemiTool-EtherCAT-WPF-ControlSuite/actions/workflows/dotnet-ci.yml/badge.svg)](https://github.com/JJY0910/SemiTool-EtherCAT-WPF-ControlSuite/actions/workflows/dotnet-ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

[Korean README](README.ko.md)

WPF/MVVM control suite for a semiconductor wafer-transfer trainer. The project keeps the approved EtherCAT teaching profile intact while providing a simulator-first HMI, a native WPF `Viewport3D` machine twin, safety interlocks, named I/O points, and automated verification around the five-wafer transfer pipeline.

The current public screenshots show the same 3D machine twin used by the running app. The old reference-photo panel is no longer part of the runtime Machine Twin view.

## Current 3D Machine Twin

| Runtime screen | Preview |
| --- | --- |
| 3D Machine Twin | ![3D Machine Twin](docs/images/machine-twin-runtime.png) |

Sequence frames captured from the WPF runtime:

- [FOUP A pickup target](docs/images/sequence-frame-01.png)
- [Blade entering Chamber A](docs/images/sequence-frame-02.png)
- [Chamber A processing](docs/images/sequence-frame-03.png)
- [FOUP B completed](docs/images/sequence-frame-04.png)

## Wafer Transfer Pipeline

`Run Transfer Sequence` drives the simulator through the full five-wafer route:

```text
FOUP A -> Chamber A -> Chamber B -> Chamber C -> FOUP B
```

| Step | Visual behavior | Verification point |
| --- | --- | --- |
| 1 | Reset or startup at `Home / Start` | Blade retracted, Z safe, FOUP A 5/5, FOUP B 0/5 |
| 2 | Move from Home to FOUP A | The blade does not extend at Home; theta targets FOUP A first |
| 3 | Move Z to the selected FOUP A slot | A1-A5 slot height is selected before extension |
| 4 | Pick wafer from FOUP A | Blade extends, suction turns on, FOUP A count decreases by one |
| 5 | Place wafer in Chamber A | Door opens, blade enters, wafer is hidden inside during processing |
| 6 | Move wafer to Chamber B | Chamber B door and process indicators track the wafer state |
| 7 | Move wafer to Chamber C | Chamber C receives the wafer after Chamber B completion |
| 8 | Place wafer in FOUP B | FOUP B fills B1-B5 in order |
| 9 | Complete cycle | FOUP A 0/5, FOUP B 5/5, right-side tower lamp shows completion |

Full simulator evidence:

- [Full pipeline QA summary](docs/debug/latest/full-pipeline/full-pipeline-qa-summary.md)
- [Full pipeline operator review](docs/debug/latest/full-pipeline/full-pipeline-operator-review.md)
- [Full pipeline contact sheet](docs/debug/latest/full-pipeline/full-pipeline-contact-sheet.png)
- `docs/debug/latest/full-pipeline/screenshots/*.png`

## Safety Boundary

The application starts in Simulator mode. It does not auto-connect, auto-run, auto-home, auto-motion, or activate outputs on startup.

Real Hardware mode requires explicit operator selection, hardware unlock, and manual Connect. The real adapter loads the vendor DLL only inside `Ieg3268EthercatController`; simulator commands and screenshot capture commands do not load the DLL and do not connect to physical EtherCAT hardware.

The repository evidence is simulator-side WPF verification only. It must not be described as real-equipment commissioning.

## Preserved Equipment Values

Do not change these values unless a newer approved `config/EquipmentProfile.finaltest.json` explicitly requires it:

- DO0-DO15 output map
- DI0-DI5 and DI12-DI13 input map
- Home, FOUP A/B, Chamber A/B/C robot poses
- FOUP slot Z safe/work values
- motion, door, cylinder, vacuum, polling, and auto tick timing values
- auto scheduler priority

The application logic uses named I/O points and the EtherCAT abstraction boundary instead of raw DO/DI channel calls.

## Build And Test

```powershell
dotnet restore SemiTool.EtherCAT.WPF.ControlSuite.sln
dotnet build SemiTool.EtherCAT.WPF.ControlSuite.sln --configuration Release --no-restore
dotnet test SemiTool.EtherCAT.WPF.ControlSuite.sln --configuration Release --no-build --no-restore
```

Regenerate GitHub-facing runtime images:

```powershell
dotnet run --project src/SemiTool.Hmi.Wpf/SemiTool.Hmi.Wpf.csproj --configuration Release -- --capture-sequence-assets
```

Regenerate detailed verification evidence:

```powershell
dotnet run --project src/SemiTool.Hmi.Wpf/SemiTool.Hmi.Wpf.csproj --configuration Release -- --capture-ui-debug-report
dotnet run --project src/SemiTool.Hmi.Wpf/SemiTool.Hmi.Wpf.csproj --configuration Release -- --capture-full-pipeline-qa
```

If Windows App Control blocks generated Release DLLs with `0x800711C7`, rerun the same command with `-p:Deterministic=false` before `--`.

## Project Layout

```text
src/SemiTool.Hmi.Wpf        WPF views, ViewModels, commands, bootstrap
src/SemiTool.Application    sequence, scheduler, alarms, interlocks, recipes, event logs
src/SemiTool.Hardware       IEthercatController, simulator, real IEG3268 adapter
src/SemiTool.Domain         equipment models, enums, profile objects
src/SemiTool.Infrastructure config/settings/profile loading and CSV support
src/SemiTool.Tests          preservation, safety, simulator, and machine-twin tests
```

## Maintainer Docs

- [Architecture](docs/architecture.md)
- [Roadmap](docs/roadmap.md)
- [Threat model](docs/threat-model.md)
- [Maintainer playbook](docs/maintainer-playbook.md)
- [Open-source readiness checklist](docs/open-source-readiness.md)
- [Contributing](CONTRIBUTING.md)
- [Security policy](SECURITY.md)
