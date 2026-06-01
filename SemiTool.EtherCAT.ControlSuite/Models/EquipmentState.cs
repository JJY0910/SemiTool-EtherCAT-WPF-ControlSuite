namespace SemiTool.EtherCAT.ControlSuite.Models;

public enum EquipmentState
{
    /// <summary>운전 가능 상태입니다. 전원/인터록 조건이 정상인 경우에 사용합니다.</summary>
    Ready,

    /// <summary>동작 또는 조건 확인이 진행 중인 상태입니다.</summary>
    Active,

    /// <summary>작업자 확인이 필요한 주의 상태입니다. 즉시 정지는 아니지만 운전 조건 확인이 필요합니다.</summary>
    Warning,

    /// <summary>비상정지, 인터록 위반 등 운전 금지 상태입니다.</summary>
    Fault,

    /// <summary>통신 또는 장비 연결이 끊긴 상태입니다.</summary>
    Offline
}
