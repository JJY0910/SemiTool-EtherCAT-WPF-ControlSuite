using SemiTool.EtherCAT.ControlSuite.Models;

namespace SemiTool.EtherCAT.ControlSuite.ViewModels;

public sealed class StationViewModel : ObservableObject
{
    private EquipmentState _state;
    private string _primaryStatus;
    private string _secondaryStatus;
    private string _interlockStatus;
    private bool _isDoorOpen;

    public StationViewModel(
        string name,
        string equipmentTag,
        string primaryStatus,
        string secondaryStatus,
        string interlockStatus,
        EquipmentState state)
    {
        Name = name;
        EquipmentTag = equipmentTag;
        _primaryStatus = primaryStatus;
        _secondaryStatus = secondaryStatus;
        _interlockStatus = interlockStatus;
        _state = state;
    }

    public string Name { get; }

    public string EquipmentTag { get; }

    public string PrimaryStatus
    {
        get => _primaryStatus;
        set => SetProperty(ref _primaryStatus, value);
    }

    public string SecondaryStatus
    {
        get => _secondaryStatus;
        set => SetProperty(ref _secondaryStatus, value);
    }

    public string InterlockStatus
    {
        get => _interlockStatus;
        set => SetProperty(ref _interlockStatus, value);
    }

    // 챔버 슬롯 도어 상태입니다. 실제 도어 출력 명령이 아니라 UI/시뮬레이션 표시값입니다.
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

    public string DoorText => IsDoorOpen ? "DOOR OPEN" : "DOOR CLOSED";

    public EquipmentState State
    {
        get => _state;
        set => SetProperty(ref _state, value);
    }
}
