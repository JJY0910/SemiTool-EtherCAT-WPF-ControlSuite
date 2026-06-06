# SemiTool EtherCAT WPF Control Suite

[![.NET CI](https://github.com/JJY0910/SemiTool-EtherCAT-WPF-ControlSuite/actions/workflows/dotnet-ci.yml/badge.svg)](https://github.com/JJY0910/SemiTool-EtherCAT-WPF-ControlSuite/actions/workflows/dotnet-ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

[English README](README.md)

반도체 웨이퍼 이송 제어 트레이너용 WPF/MVVM 제어 프로젝트입니다. 승인된 EtherCAT 티칭 프로파일 값은 그대로 보존하고, Simulator 우선 HMI, WPF `Viewport3D` 기반 3D Machine Twin, 안전 인터록, named I/O point, 5장 웨이퍼 이송 파이프라인 검증을 제공합니다.

현재 공개 스크린샷은 실행 앱에서 보이는 3D Machine Twin 화면입니다. 예전 왼쪽 상단 장비 참고 사진 패널은 런타임 Machine Twin 화면에서 제거되었습니다.

## 현재 3D Machine Twin

| 실행 화면 | 미리보기 |
| --- | --- |
| 3D Machine Twin | ![3D Machine Twin](docs/images/machine-twin-runtime.png) |

같은 WPF 런타임에서 캡처한 시퀀스 프레임:

- [FOUP A 픽업 위치](docs/images/sequence-frame-01.png)
- [Chamber A 블레이드 진입](docs/images/sequence-frame-02.png)
- [Chamber A 공정 진행](docs/images/sequence-frame-03.png)
- [FOUP B 완료 상태](docs/images/sequence-frame-04.png)

## 웨이퍼 이송 파이프라인

`Run Transfer Sequence`는 시뮬레이터에서 5장 웨이퍼를 아래 순서로 이송합니다.

```text
FOUP A -> Chamber A -> Chamber B -> Chamber C -> FOUP B
```

| 순서 | UI 동작 | 확인 포인트 |
| --- | --- | --- |
| 1 | Reset 또는 시작 시 `Home / Start` | 블레이드 retract, Z safe, FOUP A 5/5, FOUP B 0/5 |
| 2 | Home에서 FOUP A로 이동 | Home에서 바로 전진하지 않고 FOUP A 각도를 먼저 잡음 |
| 3 | 선택한 FOUP A 슬롯 높이로 Z 이동 | A1-A5 슬롯 높이에 맞춘 뒤 블레이드 전진 |
| 4 | FOUP A 웨이퍼 픽업 | 블레이드 전진, 진공 흡착, FOUP A 수량 1장 감소 |
| 5 | Chamber A 투입 | 문 열림, 블레이드 진입, 공정 중 웨이퍼는 챔버 내부에 숨김 |
| 6 | Chamber B 이송 | Chamber B 문과 공정 표시가 웨이퍼 상태와 연동 |
| 7 | Chamber C 이송 | Chamber B 완료 뒤 Chamber C로 이동 |
| 8 | FOUP B 적재 | FOUP B B1-B5 슬롯에 순서대로 적재 |
| 9 | 사이클 완료 | FOUP A 0/5, FOUP B 5/5, 오른쪽 경광봉 완료 상태 |

전체 시뮬레이터 검증 자료:

- [Full pipeline QA summary](docs/debug/latest/full-pipeline/full-pipeline-qa-summary.md)
- [Full pipeline operator review](docs/debug/latest/full-pipeline/full-pipeline-operator-review.md)
- [Full pipeline contact sheet](docs/debug/latest/full-pipeline/full-pipeline-contact-sheet.png)
- `docs/debug/latest/full-pipeline/screenshots/*.png`

## 안전 경계

앱은 Simulator mode로 시작합니다. 시작 시 자동 연결, 자동 실행, 자동 원점, 자동 모션, 출력 활성화를 하지 않습니다.

Real Hardware mode는 작업자가 명시적으로 모드를 선택하고 hardware unlock 후 수동 Connect를 눌러야 사용할 수 있습니다. vendor DLL은 `Ieg3268EthercatController` 내부에서만 로드되며, 시뮬레이터 명령과 캡처 명령은 DLL을 로드하거나 실제 EtherCAT 장비에 연결하지 않습니다.

이 저장소의 검증 자료는 시뮬레이터 기반 WPF 검증입니다. 실제 장비 commissioning 완료 자료처럼 설명하면 안 됩니다.

## 보존 장비값

새 승인본 `config/EquipmentProfile.finaltest.json`이 명시적으로 요구하지 않는 한 아래 값은 변경하지 않습니다.

- DO0-DO15 출력 맵
- DI0-DI5 및 DI12-DI13 입력 맵
- Home, FOUP A/B, Chamber A/B/C 로봇 pose
- FOUP 슬롯 Z safe/work 값
- motion, door, cylinder, vacuum, polling, auto tick timing 값
- auto scheduler priority

앱 로직은 raw DO/DI 번호 대신 named I/O point와 EtherCAT 추상화 경계를 사용합니다.

## 빌드와 테스트

```powershell
dotnet restore SemiTool.EtherCAT.WPF.ControlSuite.sln
dotnet build SemiTool.EtherCAT.WPF.ControlSuite.sln --configuration Release --no-restore
dotnet test SemiTool.EtherCAT.WPF.ControlSuite.sln --configuration Release --no-build --no-restore
```

GitHub 공개용 런타임 이미지 재생성:

```powershell
dotnet run --project src/SemiTool.Hmi.Wpf/SemiTool.Hmi.Wpf.csproj --configuration Release -- --capture-sequence-assets
```

상세 검증 자료 재생성:

```powershell
dotnet run --project src/SemiTool.Hmi.Wpf/SemiTool.Hmi.Wpf.csproj --configuration Release -- --capture-ui-debug-report
dotnet run --project src/SemiTool.Hmi.Wpf/SemiTool.Hmi.Wpf.csproj --configuration Release -- --capture-full-pipeline-qa
```

Windows App Control이 생성된 Release DLL을 `0x800711C7`로 차단하면 `--` 앞에 `-p:Deterministic=false`를 붙여 다시 실행합니다.

## 프로젝트 구조

```text
src/SemiTool.Hmi.Wpf        WPF view, ViewModel, command, bootstrap
src/SemiTool.Application    sequence, scheduler, alarm, interlock, recipe, event log
src/SemiTool.Hardware       IEthercatController, simulator, real IEG3268 adapter
src/SemiTool.Domain         장비 모델, enum, profile 객체
src/SemiTool.Infrastructure config/settings/profile loading, CSV support
src/SemiTool.Tests          보존값, 안전, 시뮬레이터, Machine Twin 테스트
```

## 유지보수 문서

- [Architecture](docs/architecture.md)
- [Roadmap](docs/roadmap.md)
- [Threat model](docs/threat-model.md)
- [Maintainer playbook](docs/maintainer-playbook.md)
- [Open-source readiness checklist](docs/open-source-readiness.md)
- [Contributing](CONTRIBUTING.md)
- [Security policy](SECURITY.md)
