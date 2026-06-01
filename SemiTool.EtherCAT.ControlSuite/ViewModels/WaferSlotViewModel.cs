using SemiTool.EtherCAT.ControlSuite.Models;

namespace SemiTool.EtherCAT.ControlSuite.ViewModels;

public sealed class WaferSlotViewModel : ObservableObject
{
    private WaferSlotState _slotState;
    private string _waferId;
    private bool _verified;
    private EquipmentState _state;

    public WaferSlotViewModel(WaferSlotSnapshot snapshot)
    {
        FoupName = snapshot.FoupName;
        SlotNumber = snapshot.SlotNumber;
        SlotLabel = snapshot.SlotLabel;
        _slotState = snapshot.State;
        _waferId = snapshot.WaferId;
        _verified = snapshot.Verified;
        _state = ToEquipmentState(snapshot.State, snapshot.Verified);
    }

    public string FoupName { get; }

    public int SlotNumber { get; }

    public string SlotLabel { get; }

    // 슬롯별 웨이퍼 감지 상태입니다. 실제 센서 연결 전에는 Unknown으로 유지합니다.
    public WaferSlotState SlotState
    {
        get => _slotState;
        private set => SetProperty(ref _slotState, value);
    }

    // 시뮬레이터 식별자 또는 실제 장비에서 읽은 웨이퍼 식별 표시값입니다.
    public string WaferId
    {
        get => _waferId;
        private set => SetProperty(ref _waferId, value);
    }

    public bool Verified
    {
        get => _verified;
        private set => SetProperty(ref _verified, value);
    }

    public EquipmentState State
    {
        get => _state;
        private set => SetProperty(ref _state, value);
    }

    public void Update(WaferSlotSnapshot snapshot)
    {
        SlotState = snapshot.State;
        WaferId = snapshot.WaferId;
        Verified = snapshot.Verified;
        State = ToEquipmentState(snapshot.State, snapshot.Verified);
    }

    private static EquipmentState ToEquipmentState(WaferSlotState state, bool verified)
    {
        if (!verified || state == WaferSlotState.Unknown)
        {
            return EquipmentState.Warning;
        }

        return state == WaferSlotState.Reserved ? EquipmentState.Active : EquipmentState.Ready;
    }
}
