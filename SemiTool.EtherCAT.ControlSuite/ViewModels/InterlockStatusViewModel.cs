using SemiTool.EtherCAT.ControlSuite.Models;

namespace SemiTool.EtherCAT.ControlSuite.ViewModels;

public sealed class InterlockStatusViewModel : ObservableObject
{
    private bool _isSatisfied;
    private string _detail;
    private InterlockSeverity _severity;
    private EquipmentState _state;

    public InterlockStatusViewModel(InterlockCheck check)
    {
        Name = check.Name;
        Source = check.Source;
        _detail = check.Detail;
        _isSatisfied = check.IsSatisfied;
        _severity = check.Severity;
        _state = ToEquipmentState(check);
    }

    public string Name { get; }

    public string Source { get; }

    public string Detail
    {
        get => _detail;
        private set => SetProperty(ref _detail, value);
    }

    public bool IsSatisfied
    {
        get => _isSatisfied;
        private set => SetProperty(ref _isSatisfied, value);
    }

    public InterlockSeverity Severity
    {
        get => _severity;
        private set => SetProperty(ref _severity, value);
    }

    public EquipmentState State
    {
        get => _state;
        private set => SetProperty(ref _state, value);
    }

    public string ResultText => IsSatisfied ? "OK" : Severity == InterlockSeverity.Blocker ? "BLOCK" : "CHECK";

    public void Update(InterlockCheck check)
    {
        Detail = check.Detail;
        IsSatisfied = check.IsSatisfied;
        Severity = check.Severity;
        State = ToEquipmentState(check);
        OnPropertyChanged(nameof(ResultText));
    }

    private static EquipmentState ToEquipmentState(InterlockCheck check)
    {
        if (check.IsSatisfied)
        {
            return EquipmentState.Ready;
        }

        return check.Severity == InterlockSeverity.Blocker ? EquipmentState.Fault : EquipmentState.Warning;
    }
}
