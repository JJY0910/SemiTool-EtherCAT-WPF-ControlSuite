# Architecture

## Layers

```text
SemiTool.Hmi.Wpf
  Views, ViewModels, commands, WPF bootstrap

SemiTool.Application
  EquipmentSequenceService, TransferScheduler, SafetyInterlockService,
  AlarmService, EventLogService, RecipeService

SemiTool.Hardware
  IEthercatController, SimulatedEthercatController,
  Ieg3268EthercatController, SelectableEthercatController

SemiTool.Domain
  EquipmentProfile, IoPoint, AxisId, RobotPose, FoupSlotPose,
  ChamberId, MachineState, AlarmCode, EquipmentStatus

SemiTool.Infrastructure
  EquipmentProfileLoader, AppSettingsStore, CSV writer
```

## Data Flow

```text
WPF HMI
  -> ViewModel command
  -> Application Service
  -> IEthercatController
  -> EtherCAT Adapter / Simulator
  -> Motion axis or Digital I/O
```

## Simulator Flow

```text
Operator selects Simulator
  -> manual Connect
  -> SimulatedEthercatController
  -> in-memory DO/DI/axis state
  -> I/O Monitor and sequence status update
```

Simulator mode is the default startup mode. It does not require vendor DLLs and never connects to real equipment.

## Real Hardware Flow

```text
Operator selects RealHardware
  -> checks hardware unlock
  -> manual Connect
  -> Ieg3268EthercatController
  -> reflection loads libs/IEG3268_Dll.dll
  -> vendor IEG3268 API
  -> real EtherCAT I/O and motion
```

The default public build compiles without the vendor DLL. Missing DLL errors are contained in real hardware connection handling.
