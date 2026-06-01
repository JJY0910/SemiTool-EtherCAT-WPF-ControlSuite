using SemiTool.EtherCAT.ControlSuite.Models;

namespace SemiTool.EtherCAT.ControlSuite.ViewModels;

public sealed class TransferStationViewModel : ObservableObject
{
    private EquipmentState _state;
    private bool _isSource;
    private bool _isTarget;
    private bool _isDoorOpen;
    private string _status;

    public TransferStationViewModel(string name, TransferStationKind kind, string status, EquipmentState state)
    {
        Name = name;
        Kind = kind;
        _status = status;
        _state = state;
    }

    public string Name { get; }

    public TransferStationKind Kind { get; }

    // 현재 선택 경로의 출발 지점입니다. 실제 위치 좌표가 아니라 UI 경로 표시용입니다.
    public bool IsSource
    {
        get => _isSource;
        set => SetProperty(ref _isSource, value);
    }

    // 현재 선택 경로의 도착 지점입니다. 실제 티칭 좌표가 아니라 UI 경로 표시용입니다.
    public bool IsTarget
    {
        get => _isTarget;
        set => SetProperty(ref _isTarget, value);
    }

    // 챔버 슬롯 도어 상태입니다. FOUP와 HOME은 항상 false로 둡니다.
    public bool IsDoorOpen
    {
        get => _isDoorOpen;
        set
        {
            if (SetProperty(ref _isDoorOpen, value))
            {
                OnPropertyChanged(nameof(DoorText));
            }
        }
    }

    public string DoorText => Kind == TransferStationKind.Chamber
        ? IsDoorOpen ? "DOOR OPEN" : "DOOR CLOSED"
        : "N/A";

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public EquipmentState State
    {
        get => _state;
        set => SetProperty(ref _state, value);
    }
}
