namespace SemiTool.Domain;

public enum OperatingMode
{
    Simulator,
    RealHardware
}

public enum IoPoint
{
    TowerRed,
    TowerYellow,
    TowerGreen,
    ChamberALamp,
    ChamberADoorClose,
    ChamberADoorOpen,
    ChamberBLamp,
    ChamberBDoorClose,
    ChamberBDoorOpen,
    ChamberCLamp,
    ChamberCDoorClose,
    ChamberCDoorOpen,
    CylinderForward,
    CylinderBackward,
    VacuumSuction,
    VacuumExhaust,
    ChamberADoorOpenSensor,
    ChamberADoorCloseSensor,
    ChamberBDoorOpenSensor,
    ChamberBDoorCloseSensor,
    ChamberCDoorOpenSensor,
    ChamberCDoorCloseSensor,
    CylinderRearSensor,
    CylinderFrontSensor
}

public enum AxisId
{
    Z,
    Theta
}

public enum ChamberId
{
    A,
    B,
    C
}

public enum MachineState
{
    Offline,
    Idle,
    Manual,
    AutoRunning,
    Paused,
    Alarm,
    Emergency
}

public enum AlarmCode
{
    None = 0,
    NotConnected = 100,
    HardwareNotUnlocked = 110,
    HomingRequired = 120,
    ManualBlockedDuringAuto = 130,
    Timeout = 200,
    DoorSensorMismatch = 210,
    CylinderTimeout = 220,
    CommunicationFailure = 300,
    EmergencyStop = 900,
    SequenceFailed = 910
}

public enum EquipmentCommand
{
    Connect,
    Disconnect,
    StartAuto,
    StopAuto,
    PauseAuto,
    Reset,
    EmergencyStop,
    ServoOn,
    ServoOff,
    HomeZ,
    HomeTheta,
    MoveAxis,
    MoveToPose,
    SetOutput
}

public enum TransferActionKind
{
    None,
    PmCToFoupB,
    PmBToPmC,
    PmAToPmB,
    FoupAToPmA,
    ProcessTick
}
