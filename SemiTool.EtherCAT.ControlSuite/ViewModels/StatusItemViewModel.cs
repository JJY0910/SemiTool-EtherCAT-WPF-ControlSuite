using SemiTool.EtherCAT.ControlSuite.Models;

namespace SemiTool.EtherCAT.ControlSuite.ViewModels;

public sealed class StatusItemViewModel : ObservableObject
{
    private EquipmentState _state;
    private string _value;
    private string _detail;

    public StatusItemViewModel(string name, string value, string detail, EquipmentState state)
    {
        Name = name;
        _value = value;
        _detail = detail;
        _state = state;
    }

    public string Name { get; }

    public string Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }

    public string Detail
    {
        get => _detail;
        set => SetProperty(ref _detail, value);
    }

    public EquipmentState State
    {
        get => _state;
        set => SetProperty(ref _state, value);
    }
}
