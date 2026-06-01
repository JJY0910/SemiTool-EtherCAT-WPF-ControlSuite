using SemiTool.EtherCAT.ControlSuite.Models;

namespace SemiTool.EtherCAT.ControlSuite.ViewModels;

public sealed class AlarmEventViewModel : ObservableObject
{
    private EquipmentState _state;
    private string _message;

    public AlarmEventViewModel(DateTimeOffset timestamp, string source, string message, EquipmentState state)
    {
        Timestamp = timestamp;
        Source = source;
        _message = message;
        _state = state;
    }

    public DateTimeOffset Timestamp { get; }

    public string TimeText => Timestamp.ToLocalTime().ToString("HH:mm:ss");

    public string Source { get; }

    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    public EquipmentState State
    {
        get => _state;
        set => SetProperty(ref _state, value);
    }
}
