namespace SemiTool.EtherCAT.ControlSuite.Models;

public sealed record EquipmentSnapshot(
    int Cycle,
    bool EtherCatLink,
    bool ServoReady,
    bool AxisHomed,
    bool EmergencyStop,
    bool ChamberDoorsClosed,
    bool FoupCassettePresent,
    bool SlotMapVerified,
    bool VacuumReady,
    bool RouteClear,
    int SequenceProgress,
    string MotionPhase,
    IReadOnlyList<WaferSlotSnapshot> SlotMap);
