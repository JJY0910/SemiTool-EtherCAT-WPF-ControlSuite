using SemiTool.EtherCAT.ControlSuite.Models;

namespace SemiTool.EtherCAT.ControlSuite.ViewModels;

public sealed class StationViewModel : ObservableObject
{
    private EquipmentState _state;
    private string _primaryStatus;
    private string _secondaryStatus;
    private string _interlockStatus;

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

    public EquipmentState State
    {
        get => _state;
        set => SetProperty(ref _state, value);
    }
}
