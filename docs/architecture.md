# Architecture

SemiTool EtherCAT WPF Control Suite is structured around a safety-first WPF/MVVM boundary. The HMI can run fully in Simulator mode, while the real EtherCAT path remains behind an explicit operator-controlled adapter.

## Layer Map

```text
SemiTool.Hmi.Wpf
  WPF views, ViewModels, commands, startup, design-time previews

SemiTool.Application
  EquipmentSequenceService, RuntimeCoordinator, TransferScheduler,
  SafetyInterlockService, AlarmService, EventLogService, RecipeService

SemiTool.Hardware
  IEthercatController, SimulatedEthercatController,
  SelectableEthercatController, Ieg3268EthercatController

SemiTool.Domain
  EquipmentProfile, IoPoint, AxisId, RobotPose, FoupSlotPose,
  ChamberId, MachineState, AlarmCode, EquipmentStatus

SemiTool.Infrastructure
  EquipmentProfileLoader, AppSettingsStore, CSV and profile support
```

## Runtime Flow

```text
Operator command
  -> WPF ViewModel
  -> RuntimeCoordinator / Application service
  -> SafetyInterlockService
  -> IEthercatController
  -> Simulator or real IEG3268 adapter
  -> status, alarms, event log, Machine Twin
```

The ViewModel layer should bind to state and commands. It should not know raw hardware channels or vendor DLL details.

## Startup Safety Boundary

Startup creates the equipment profile, controller selector, services, and main window. It must not connect to hardware, start auto sequence, home axes, move axes, or activate outputs.

Simulator mode is the default operator mode. Real Hardware mode requires explicit mode selection, hardware unlock, and manual Connect.

## Simulator Path

```text
Simulator selected
  -> operator presses Connect
  -> SimulatedEthercatController
  -> in-memory axes and I/O
  -> HMI status, event log, Machine Twin
```

The simulator is used for normal development, CI tests, and screenshot capture. It does not load the vendor DLL.

## Real Hardware Path

```text
Real Hardware selected
  -> hardware unlock confirmed
  -> operator presses Connect
  -> Ieg3268EthercatController
  -> libs/IEG3268_Dll.dll loaded by adapter only
  -> real EtherCAT I/O and motion
```

The public build can compile without the vendor DLL. Missing DLL errors are contained in the real-hardware connection path.

## 3D Machine Twin

The Machine Twin is a native WPF `Viewport3D` visual layer driven by simulator sequence state:

- FOUP A starts with five slots populated.
- The robot stays at `Home / Start` with the blade retracted until a station target is selected.
- FOUP A and FOUP B station angles are display angles only; they do not rewrite preserved encoder teaching values.
- Chamber doors, button lamps, wafer ownership, blade extension, Z state, and tower lamp state are derived from the transfer sequence snapshot.
- Wafers placed inside a chamber are tracked logically while hidden from the outside view during processing.

## Preserved Values

`config/EquipmentProfile.finaltest.json` is the authority for protected hardware values. Tests should protect:

- DO/DI maps
- robot station poses
- FOUP slot Z safe/work values
- timing and scheduler priority
- named I/O usage in application logic

UI layout or simulator display improvements must not rewrite the approved profile.
