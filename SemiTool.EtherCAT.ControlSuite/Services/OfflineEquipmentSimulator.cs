using SemiTool.EtherCAT.ControlSuite.Models;

namespace SemiTool.EtherCAT.ControlSuite.Services;

public sealed class OfflineEquipmentSimulator
{
    private int _cycle;
    private int _sequenceProgress;
    private bool _etherCatLink;
    private bool _servoReady;
    private bool _axisHomed;
    private bool _emergencyStop;
    private bool _chamberDoorsClosed = true;
    private bool _slotMapVerified;
    private bool _vacuumReady;
    private string _motionPhase = "Home ready";

    public EquipmentSnapshot CreatePowerOnSnapshot()
    {
        _cycle = 0;
        _sequenceProgress = 0;
        _etherCatLink = false;
        _servoReady = false;
        _axisHomed = false;
        _emergencyStop = false;
        _chamberDoorsClosed = true;
        _slotMapVerified = false;
        _vacuumReady = false;
        _motionPhase = "Power on wait";

        return BuildSnapshot();
    }

    public EquipmentSnapshot ConnectOfflineRig()
    {
        _etherCatLink = true;
        _servoReady = true;
        _axisHomed = true;
        _vacuumReady = true;
        _motionPhase = "Home ready - simulator connected";

        return BuildSnapshot();
    }

    public EquipmentSnapshot VerifySlotMap()
    {
        _slotMapVerified = true;
        _motionPhase = "Pipeline ready - slot map verified";

        return BuildSnapshot();
    }

    public EquipmentSnapshot AdvanceCycle(string selectedRoute)
    {
        _cycle++;

        if (_emergencyStop)
        {
            _sequenceProgress = 0;
            _motionPhase = "Blocked - emergency stop";
            return BuildSnapshot();
        }

        if (!_etherCatLink || !_servoReady || !_axisHomed || !_vacuumReady)
        {
            _sequenceProgress = 0;
            _motionPhase = "Blocked - hardware interlock wait";
            return BuildSnapshot();
        }

        if (!_chamberDoorsClosed)
        {
            _sequenceProgress = 0;
            _motionPhase = "Blocked - chamber door open";
            return BuildSnapshot();
        }

        if (!_slotMapVerified)
        {
            _sequenceProgress = 0;
            _motionPhase = "Blocked - verify FOUP slot map first";
            return BuildSnapshot();
        }

        _sequenceProgress = _sequenceProgress switch
        {
            < 12 => 12,
            < 25 => 25,
            < 38 => 38,
            < 52 => 52,
            < 68 => 68,
            < 82 => 82,
            < 94 => 94,
            < 100 => 100,
            _ => 100
        };

        _motionPhase = _sequenceProgress switch
        {
            12 => $"{selectedRoute} | rotate from HOME to source",
            25 => $"{selectedRoute} | extend blade into source slot",
            38 => $"{selectedRoute} | vacuum pickup and wafer clamp",
            52 => $"{selectedRoute} | retract blade with wafer",
            68 => $"{selectedRoute} | rotate to destination chamber",
            82 => $"{selectedRoute} | extend blade into chamber slot",
            94 => $"{selectedRoute} | release wafer at destination",
            _ => $"{selectedRoute} | retract to HOME complete"
        };

        return BuildSnapshot();
    }

    public EquipmentSnapshot SetEmergencyStop(bool isPressed)
    {
        _emergencyStop = isPressed;
        _sequenceProgress = isPressed ? 0 : _sequenceProgress;
        _motionPhase = isPressed ? "Emergency stop active" : "Emergency stop released";

        return BuildSnapshot();
    }

    public EquipmentSnapshot SetChamberDoorOpen(bool isOpen)
    {
        _chamberDoorsClosed = !isOpen;
        _sequenceProgress = isOpen ? 0 : _sequenceProgress;
        _motionPhase = isOpen ? "Chamber door open - motion blocked" : "Chamber door closed - recheck interlocks";

        return BuildSnapshot();
    }

    private EquipmentSnapshot BuildSnapshot()
    {
        return new EquipmentSnapshot(
            _cycle,
            _etherCatLink,
            _servoReady,
            _axisHomed,
            _emergencyStop,
            ChamberDoorsClosed: _chamberDoorsClosed,
            FoupCassettePresent: true,
            _slotMapVerified,
            _vacuumReady,
            RouteClear: !_emergencyStop && _chamberDoorsClosed,
            _sequenceProgress,
            _motionPhase,
            BuildSlotMap());
    }

    private IReadOnlyList<WaferSlotSnapshot> BuildSlotMap()
    {
        var verified = _slotMapVerified;

        // SIM-* values are local simulator identifiers only.
        // They are not real wafer IDs, teaching coordinates, offsets, or equipment parameters.
        return new[]
        {
            new WaferSlotSnapshot("FOUP A", 1, verified ? WaferSlotState.Occupied : WaferSlotState.Unknown, verified ? "SIM-A-01" : "N/A", verified),
            new WaferSlotSnapshot("FOUP A", 2, verified ? WaferSlotState.Occupied : WaferSlotState.Unknown, verified ? "SIM-A-02" : "N/A", verified),
            new WaferSlotSnapshot("FOUP A", 3, verified ? WaferSlotState.Reserved : WaferSlotState.Unknown, verified ? "SIM-A-03" : "N/A", verified),
            new WaferSlotSnapshot("FOUP A", 4, verified ? WaferSlotState.Occupied : WaferSlotState.Unknown, verified ? "SIM-A-04" : "N/A", verified),
            new WaferSlotSnapshot("FOUP A", 5, verified ? WaferSlotState.Occupied : WaferSlotState.Unknown, verified ? "SIM-A-05" : "N/A", verified),
            new WaferSlotSnapshot("FOUP B", 1, verified ? WaferSlotState.Empty : WaferSlotState.Unknown, verified ? "EMPTY" : "N/A", verified),
            new WaferSlotSnapshot("FOUP B", 2, verified ? WaferSlotState.Empty : WaferSlotState.Unknown, verified ? "EMPTY" : "N/A", verified),
            new WaferSlotSnapshot("FOUP B", 3, verified ? WaferSlotState.Empty : WaferSlotState.Unknown, verified ? "EMPTY" : "N/A", verified),
            new WaferSlotSnapshot("FOUP B", 4, verified ? WaferSlotState.Empty : WaferSlotState.Unknown, verified ? "EMPTY" : "N/A", verified),
            new WaferSlotSnapshot("FOUP B", 5, verified ? WaferSlotState.Empty : WaferSlotState.Unknown, verified ? "EMPTY" : "N/A", verified)
        };
    }
}
