namespace SemiTool.EtherCAT.ControlSuite.Models;

public enum WaferSlotState
{
    /// <summary>슬롯 상태를 아직 센서 또는 슬롯맵에서 확인하지 못했습니다.</summary>
    Unknown,

    /// <summary>슬롯이 비어 있습니다.</summary>
    Empty,

    /// <summary>슬롯에 웨이퍼가 감지되었습니다.</summary>
    Occupied,

    /// <summary>현재 선택된 이송 경로에서 사용할 슬롯입니다.</summary>
    Reserved
}
