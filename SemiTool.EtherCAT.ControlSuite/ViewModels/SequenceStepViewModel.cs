using SemiTool.EtherCAT.ControlSuite.Models;

namespace SemiTool.EtherCAT.ControlSuite.ViewModels;

public sealed class SequenceStepViewModel : ObservableObject
{
    private EquipmentState _state;
    private string _status;

    public SequenceStepViewModel(int stepNo, string name, string status, EquipmentState state)
    {
        StepNo = stepNo;
        Name = name;
        _status = status;
        _state = state;
    }

    public int StepNo { get; }

    public string Name { get; }

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
