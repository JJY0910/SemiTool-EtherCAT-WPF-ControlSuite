namespace SemiTool.EtherCAT.ControlSuite.Models;

public enum InterlockSeverity
{
    /// <summary>정보성 확인 항목입니다.</summary>
    Info,

    /// <summary>작업자 확인이 필요한 항목입니다.</summary>
    Warning,

    /// <summary>실제 장비 이동 명령을 막아야 하는 항목입니다.</summary>
    Blocker
}
