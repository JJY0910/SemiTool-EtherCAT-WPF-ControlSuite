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
    private string _motionPhase = "초기 대기";

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
        _motionPhase = "전원 투입 대기";

        return BuildSnapshot();
    }

    public EquipmentSnapshot ConnectOfflineRig()
    {
        _etherCatLink = true;
        _servoReady = true;
        _axisHomed = true;
        _vacuumReady = true;
        _motionPhase = "오프라인 시뮬레이터 연결";

        return BuildSnapshot();
    }

    public EquipmentSnapshot VerifySlotMap()
    {
        _slotMapVerified = true;
        _motionPhase = "슬롯맵 검증 완료";

        return BuildSnapshot();
    }

    public EquipmentSnapshot AdvanceCycle(string selectedRoute)
    {
        _cycle++;

        if (_emergencyStop)
        {
            _sequenceProgress = 0;
            _motionPhase = "비상정지 유지";
            return BuildSnapshot();
        }

        if (!_etherCatLink || !_servoReady || !_axisHomed || !_vacuumReady)
        {
            _motionPhase = "인터록 대기";
            _sequenceProgress = Math.Min(_sequenceProgress, 15);
            return BuildSnapshot();
        }

        _sequenceProgress = _sequenceProgress switch
        {
            < 20 => 20,
            < 42 => 42,
            < 64 => 64,
            < 86 => 86,
            _ => 100
        };

        _motionPhase = _sequenceProgress switch
        {
            20 => $"{selectedRoute} 슬롯 확인",
            42 => $"{selectedRoute} 픽업 헤드 접근",
            64 => $"{selectedRoute} 진공 흡착 확인",
            86 => $"{selectedRoute} 회전/선형 이송 확인",
            _ => $"{selectedRoute} 오프라인 사이클 완료"
        };

        return BuildSnapshot();
    }

    public EquipmentSnapshot SetEmergencyStop(bool isPressed)
    {
        _emergencyStop = isPressed;
        _sequenceProgress = isPressed ? 0 : _sequenceProgress;
        _motionPhase = isPressed ? "비상정지 입력" : "비상정지 해제";

        return BuildSnapshot();
    }

    public EquipmentSnapshot SetChamberDoorOpen(bool isOpen)
    {
        _chamberDoorsClosed = !isOpen;
        _motionPhase = isOpen ? "챔버 도어 열림 - 이송 금지" : "챔버 도어 닫힘 - 인터록 재확인";

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
            RouteClear: !_emergencyStop,
            _sequenceProgress,
            _motionPhase,
            BuildSlotMap());
    }

    private IReadOnlyList<WaferSlotSnapshot> BuildSlotMap()
    {
        var verified = _slotMapVerified;

        // SIM-* 표기는 집/개발 환경에서 쓰는 시뮬레이터 슬롯 식별자입니다.
        // 실제 웨이퍼 ID나 티칭 좌표가 아니며, 실장비 연결 시 PLC/센서 데이터로 교체해야 합니다.
        return new[]
        {
            new WaferSlotSnapshot("FOUP A", 1, verified ? WaferSlotState.Occupied : WaferSlotState.Unknown, verified ? "SIM-A-01" : "N/A", verified),
            new WaferSlotSnapshot("FOUP A", 2, verified ? WaferSlotState.Empty : WaferSlotState.Unknown, verified ? "EMPTY" : "N/A", verified),
            new WaferSlotSnapshot("FOUP A", 3, verified ? WaferSlotState.Reserved : WaferSlotState.Unknown, verified ? "SIM-A-03" : "N/A", verified),
            new WaferSlotSnapshot("FOUP A", 4, verified ? WaferSlotState.Empty : WaferSlotState.Unknown, verified ? "EMPTY" : "N/A", verified),
            new WaferSlotSnapshot("FOUP A", 5, verified ? WaferSlotState.Occupied : WaferSlotState.Unknown, verified ? "SIM-A-05" : "N/A", verified),
            new WaferSlotSnapshot("FOUP B", 1, verified ? WaferSlotState.Empty : WaferSlotState.Unknown, verified ? "EMPTY" : "N/A", verified),
            new WaferSlotSnapshot("FOUP B", 2, verified ? WaferSlotState.Occupied : WaferSlotState.Unknown, verified ? "SIM-B-02" : "N/A", verified),
            new WaferSlotSnapshot("FOUP B", 3, verified ? WaferSlotState.Empty : WaferSlotState.Unknown, verified ? "EMPTY" : "N/A", verified),
            new WaferSlotSnapshot("FOUP B", 4, verified ? WaferSlotState.Occupied : WaferSlotState.Unknown, verified ? "SIM-B-04" : "N/A", verified),
            new WaferSlotSnapshot("FOUP B", 5, verified ? WaferSlotState.Empty : WaferSlotState.Unknown, verified ? "EMPTY" : "N/A", verified)
        };
    }
}
