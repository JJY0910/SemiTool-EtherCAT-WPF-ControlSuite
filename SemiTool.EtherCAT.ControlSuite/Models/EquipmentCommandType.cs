namespace SemiTool.EtherCAT.ControlSuite.Models;

public enum EquipmentCommandType
{
    /// <summary>실장비 통신 상태만 확인하는 명령입니다.</summary>
    CheckConnection,

    /// <summary>서보 준비 상태를 확인하는 명령입니다. 실제 Servo ON 출력은 어댑터 구현에서 별도 승인 후 처리합니다.</summary>
    CheckServoReady,

    /// <summary>원점 완료 조건을 확인하는 명령입니다.</summary>
    CheckHome,

    /// <summary>FOUP 슬롯맵을 읽는 명령입니다.</summary>
    ReadSlotMap,

    /// <summary>오프라인 시뮬레이터에서만 허용하는 이송 시퀀스 진행 명령입니다.</summary>
    AdvanceOfflineSimulation,

    /// <summary>실제 장비 이송 명령입니다. 승인 티칭값과 모든 인터록이 만족되기 전에는 차단합니다.</summary>
    IssueRealMotion,

    /// <summary>비상정지 또는 정지 상태 확인 명령입니다.</summary>
    StopMotion
}
