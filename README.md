# SemiTool-EtherCAT-WPF-ControlSuite

Clean WPF/MVVM semiconductor equipment-control HMI and sequence platform rebuilt from a legacy WinForms EtherCAT project that successfully controlled real hardware.

This is not a WinForms screen conversion. The new solution separates HMI, application sequence logic, hardware abstraction, simulator behavior, real EtherCAT adapter loading, profile-based equipment values, alarms, interlocks, logs, recipes, and wafer transfer flow.

## Portfolio Highlights

- Real EtherCAT equipment-control experience base
- WPF/MVVM redesign instead of one-to-one WinForms screen conversion
- Simulator-first safe startup with no auto-connect, no auto-run, no auto-motion, and all outputs off
- Real hardware adapter isolated behind `IEthercatController`
- Preserved DO/DI, robot pose, FOUP slot, and timing values in `EquipmentProfile`
- Async sequence logic with cancellation, timeout, and alarm handling
- Unit tests for preserved values, simulator behavior, safety blocking, timeout alarms, and scheduler priority
- Public repo excludes vendor DLLs, generated binaries, and legacy binary outputs

## Current Verification Status

- Build: passed locally
- Tests: 16 passed locally
- GitHub Actions: enabled after this change
- Simulator mode: ready for developer PC verification
- Real hardware mode: requires local `IEG3268_Dll.dll` and school equipment verification

## Demo Plan

- Simulator demo GIF: `docs/images/simulator-demo.gif`
- Real hardware short video plan after supervised commissioning
- I/O monitor screenshot: `docs/images/io-monitor.png`
- Auto sequence screenshot: `docs/images/auto-sequence.png`
- Alarm/reset screenshot: `docs/images/alarm-log.png`

## TODO: Actual Equipment Verification

- TODO(real-hardware): Run the commissioning checklist on the school equipment with E-stop supervision.
- TODO(real-hardware): Capture an approved short real hardware verification video.
- TODO(real-hardware): Add approved screenshots/GIFs to `docs/images/` after simulator and hardware checks.

## Why This Is Real Equipment-Control Related

The preserved I/O map, Z/Theta robot poses, FOUP slot positions, timing constants, and transfer priority came from the original EtherCAT equipment-control project. The new project keeps those values in `config/EquipmentProfile.finaltest.json` and protects them with unit tests.

The app starts in Simulator mode, with no auto-connect, no auto-run, no automatic motion, and all outputs off. Real Hardware mode is present, but the vendor DLL is loaded only by `Ieg3268EthercatController` at runtime.

## Architecture

```text
WPF HMI
  -> ViewModels / Commands
  -> Application Services
  -> IEthercatController
  -> SimulatedEthercatController OR Ieg3268EthercatController
  -> Digital I/O, motion axes, cylinder, vacuum, doors, lamps
```

Projects:

```text
src/SemiTool.Hmi.Wpf        WPF views, ViewModels, commands, bootstrap
src/SemiTool.Domain         Equipment models, enums, profile objects
src/SemiTool.Application    Sequence, scheduler, alarms, interlocks, recipes, event logs
src/SemiTool.Hardware       IEthercatController, simulator, real IEG3268 adapter
src/SemiTool.Infrastructure Config/settings/profile loading and CSV support
src/SemiTool.Tests          Value preservation and behavior tests
```

## Screens

- Dashboard
- Manual Control
- I/O Monitor
- Auto Sequence
- Wafer / Recipe Flow
- Alarm & Event Log
- Settings

## Simulator Mode

```powershell
dotnet run --project src/SemiTool.Hmi.Wpf/SemiTool.Hmi.Wpf.csproj
```

Startup mode is Simulator. Click `Connect`, then use Manual Control for servo/home/move/output checks. Simulator input states can be toggled in the I/O Monitor.

## Real Hardware Mode

1. Keep vendor DLLs out of git.
2. Place the local vendor DLL at `libs/IEG3268_Dll.dll` or set the path in Settings.
3. In Settings, select `RealHardware`.
4. Check the hardware unlock option.
5. Apply settings.
6. Click `Connect` manually.

If the DLL is missing, Real Hardware mode reports a clear connection error and Simulator mode remains usable.

## Safety Warning

Real hardware mode can move axes and actuate outputs. Use only with the correct machine, verified wiring, E-stop path, and operator supervision. Disconnect, Emergency Stop, communication failure, or fatal alarm should stop motion where possible and turn off risky outputs.

## Preserved Hardware Values Summary

Digital outputs:

```text
DO0 Tower Red, DO1 Tower Yellow, DO2 Tower Green
DO3 Chamber A Lamp, DO4 Chamber A Door Close, DO5 Chamber A Door Open
DO6 Chamber B Lamp, DO7 Chamber B Door Close, DO8 Chamber B Door Open
DO9 Chamber C Lamp, DO10 Chamber C Door Close, DO11 Chamber C Door Open
DO12 Cylinder Forward, DO13 Cylinder Backward, DO14 Vacuum Suction, DO15 Vacuum Exhaust
```

Digital inputs:

```text
DI0 A Door Open, DI1 A Door Close, DI2 B Door Open, DI3 B Door Close
DI4 C Door Open, DI5 C Door Close, DI12 Cylinder Rear, DI13 Cylinder Front
```

Robot poses, FOUP slot Z values, timing constants, and recipes are preserved in `config/EquipmentProfile.finaltest.json` and covered by tests.

## Build / Test

```powershell
dotnet restore SemiTool.EtherCAT.WPF.ControlSuite.sln
dotnet build SemiTool.EtherCAT.WPF.ControlSuite.sln
dotnet test SemiTool.EtherCAT.WPF.ControlSuite.sln --no-build --no-restore
```

## Portfolio Explanation

This project demonstrates how real WinForms-based EtherCAT equipment-control experience can be redesigned into a safer WPF/MVVM architecture. It keeps the equipment constants that mattered on hardware, while introducing simulator-first startup, named I/O points, async sequences, interlocks, alarm logging, and a reflection-isolated real hardware adapter.

## Interview Explanation

English:

```text
My previous project controlled real EtherCAT equipment from a WinForms application. For this portfolio project I redesigned that experience as a clean WPF/MVVM control suite. The preserved digital I/O map, Z/Theta poses, FOUP slot positions, timing values, and transfer priority are stored in an EquipmentProfile JSON file and protected by unit tests. The UI talks to ViewModels, the ViewModels call application services, and all hardware access goes through IEthercatController. Simulator mode runs on any developer PC, while real hardware mode is isolated behind a runtime-loaded IEG3268 adapter.
```

Korean:

```text
기존에는 WinForms 기반 프로그램으로 실제 EtherCAT 장비를 제어한 경험이 있습니다. 이 프로젝트에서는 그 경험을 바탕으로 WPF/MVVM 구조로 새롭게 재설계했습니다. 실제 장비에서 사용했던 DO/DI 맵, Z/Theta 포즈, FOUP 슬롯 위치, 타이밍 값, 이송 우선순위를 EquipmentProfile JSON으로 분리했고 단위 테스트로 보존값을 검증합니다. UI는 ViewModel을 통해 Application Service를 호출하고, 실제 하드웨어 접근은 IEthercatController 인터페이스 뒤로 격리했습니다. 하드웨어가 없는 PC에서는 Simulator 모드로 동작하고, 실제 장비 모드는 IEG3268 어댑터에서 vendor DLL을 런타임에 로드하도록 분리했습니다.
```
