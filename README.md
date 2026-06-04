# SemiTool-EtherCAT-WPF-ControlSuite

[![.NET CI](https://github.com/JJY0910/SemiTool-EtherCAT-WPF-ControlSuite/actions/workflows/dotnet-ci.yml/badge.svg)](https://github.com/JJY0910/SemiTool-EtherCAT-WPF-ControlSuite/actions/workflows/dotnet-ci.yml)

[한국어 README](README.ko.md)

## Current 3D Machine Twin

The public screenshots now use the native WPF `Viewport3D` Machine Twin that is shown in the running app. The old reference-photo panel is not part of the runtime view.

## 현재 구현된 3D 파이프라인 설명

GitHub 첫 화면에서 바로 확인할 수 있도록 현재 WPF 앱의 3D Machine Twin 동작 기준을 정리했습니다.

| 순서 | UI 동작 | 확인 포인트 |
| --- | --- | --- |
| 1 | `Home / Start` 안전 위치 | 블레이드는 접힌 상태, Z Safe, FOUP A 5장 / FOUP B 0장 |
| 2 | FOUP A 슬롯 선택 | Home에서 바로 전진하지 않고 FOUP A 각도(`-120 deg`)로 먼저 회전 |
| 3 | 슬롯 높이 이동 | A1~A5 각 슬롯에 맞춰 Z Work 위치로 이동 |
| 4 | FOUP A 픽업 | 블레이드 전진, 진공 흡착, FOUP A 카운트 1장 감소 |
| 5 | Chamber A 투입 및 공정 | 문 열림, 블레이드 진입, 웨이퍼는 챔버 내부로 숨김, 문 닫힘 후 공정 |
| 6 | Chamber B 이동 및 공정 | Chamber A 완료 후 B로 이송, B 내부 공정 상태 유지 |
| 7 | Chamber C 이동 및 공정 | Chamber B 완료 후 C로 이송, C 내부 공정 상태 유지 |
| 8 | FOUP B 적재 | Chamber C 완료 후 FOUP B 슬롯 B1~B5에 순서대로 적재 |
| 9 | 완료 상태 | FOUP A 0/5, FOUP B 5/5, 오른쪽 경광등 노란 완료 상태 |

전체 5장 파이프라인 검증 자료:

- [Full pipeline QA summary](docs/debug/latest/full-pipeline/full-pipeline-qa-summary.md)
- [Full pipeline operator review](docs/debug/latest/full-pipeline/full-pipeline-operator-review.md)
- [Full pipeline contact sheet](docs/debug/latest/full-pipeline/full-pipeline-contact-sheet.png)

| Runtime screen | Preview |
| --- | --- |
| 3D Machine Twin | ![3D Machine Twin](docs/images/machine-twin-runtime.png) |

Sequence frames from the same WPF view:

- [FOUP A pickup target](docs/images/sequence-frame-01.png)
- [Blade entering Chamber A](docs/images/sequence-frame-02.png)
- [Chamber A processing](docs/images/sequence-frame-03.png)
- [FOUP B completed](docs/images/sequence-frame-04.png)

## Machine Twin Behavior

`Run Transfer Sequence` drives a five-wafer simulator pipeline:

```text
FOUP A -> Chamber A -> Chamber B -> Chamber C -> FOUP B
```

The visual sequence is intentionally station-gated:

- reset returns to `Home / Start` with the blade retracted.
- pickup moves from Home to the FOUP A station angle before Z Work and blade extension.
- FOUP A drains from five wafers to zero, one slot at a time.
- Chamber A/B/C wafers are hidden inside the chamber after placement while processing continues.
- chamber buttons stay green while a chamber door is open or a chamber contains a wafer.
- FOUP B fills from zero wafers to five.
- the right-side tower lamp is the runtime status lamp: green while running, red while paused/stopped by operator action, yellow when the pipeline is complete.

The visual angle is an HMI display angle only. Preserved theta encoder values from `config/EquipmentProfile.finaltest.json` are not converted into new hardware teaching values.

## Verification Evidence

Latest simulator-only evidence:

- [Runtime verification README](docs/debug/latest/runtime-verification/README.md)
- [UI runtime verification report](docs/debug/latest/ui-runtime-verification.md)
- [Full pipeline operator review](docs/debug/latest/full-pipeline/full-pipeline-operator-review.md)
- [Full pipeline QA summary](docs/debug/latest/full-pipeline/full-pipeline-qa-summary.md)
- `docs/debug/latest/runtime-verification/dev-actual/*.png`
- `docs/debug/latest/full-pipeline/screenshots/*.png`

The local verification captures were generated from `C:\dev\SemiTool-EtherCAT-WPF-ControlSuite`, which is the active development checkout for this project.

## Screens

Additional HMI screens are still available:

| Screen | Preview |
| --- | --- |
| Dashboard | ![Dashboard](docs/images/dashboard.png) |
| Manual Control | ![Manual Control](docs/images/manual-control.png) |
| I/O Monitor | ![I/O Monitor](docs/images/io-monitor.png) |
| Auto Sequence | ![Auto Sequence](docs/images/auto-sequence.png) |
| Wafer / Recipe Flow | ![Wafer / Recipe Flow](docs/images/wafer-flow.png) |
| Alarm & Event Log | ![Alarm & Event Log](docs/images/alarm-log.png) |
| Settings | ![Settings](docs/images/settings.png) |

## Safety Boundary

The WPF app starts in Simulator mode. It does not auto-connect, auto-run, auto-home, auto-motion, or activate outputs on startup.

Real Hardware mode is available only through explicit operator selection, unlock, and manual Connect. The real adapter loads the vendor DLL only inside `Ieg3268EthercatController`; simulator capture commands do not load the DLL and do not connect to physical EtherCAT hardware.

The new WPF implementation has not been verified on the school equipment in this repository state. Do not describe simulator evidence as real-hardware commissioning.

## Preserved Equipment Values

These values are protected and must not be changed unless a newer approved `EquipmentProfile.finaltest.json` requires it:

- DO0-DO15 output map
- DI0-DI5 and DI12-DI13 input map
- Home, FOUP A/B, Chamber A/B/C robot poses
- FOUP slot Z safe/work values
- motion, door, cylinder, vacuum, polling, and auto tick timing values
- auto scheduler priority

The HMI uses named I/O points and the hardware abstraction boundary instead of raw DO/DI channel calls.

## Build, Test, And Capture

```powershell
dotnet restore SemiTool.EtherCAT.WPF.ControlSuite.sln
dotnet build SemiTool.EtherCAT.WPF.ControlSuite.sln --configuration Debug
dotnet test SemiTool.EtherCAT.WPF.ControlSuite.sln --configuration Debug --no-build --no-restore
```

Regenerate GitHub-facing images:

```powershell
dotnet run --project src/SemiTool.Hmi.Wpf/SemiTool.Hmi.Wpf.csproj --configuration Release -- --capture-sequence-assets
```

If Windows App Control blocks generated Release DLLs with `0x800711C7`, rerun the same command with `-p:Deterministic=false` before `--`.

Regenerate verification evidence:

```powershell
dotnet run --project src/SemiTool.Hmi.Wpf/SemiTool.Hmi.Wpf.csproj --configuration Release -- --capture-ui-debug-report
dotnet run --project src/SemiTool.Hmi.Wpf/SemiTool.Hmi.Wpf.csproj --configuration Release -- --capture-full-pipeline-qa
```

## Project Layout

```text
src/SemiTool.Hmi.Wpf        WPF views, ViewModels, commands, bootstrap
src/SemiTool.Domain         Equipment models, enums, profile objects
src/SemiTool.Application    Sequence, scheduler, alarms, interlocks, recipes, event logs
src/SemiTool.Hardware       IEthercatController, simulator, real IEG3268 adapter
src/SemiTool.Infrastructure Config/settings/profile loading and CSV support
src/SemiTool.Tests          Value preservation and behavior tests
```
