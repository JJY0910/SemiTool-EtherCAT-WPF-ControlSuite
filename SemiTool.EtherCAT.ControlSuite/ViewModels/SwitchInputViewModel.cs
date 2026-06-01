using SemiTool.EtherCAT.ControlSuite.Models;

namespace SemiTool.EtherCAT.ControlSuite.ViewModels;

public sealed class SwitchInputViewModel : ObservableObject
{
    private bool _isPressed;
    private EquipmentState _state;
    private string _detail;

    public SwitchInputViewModel(string name, string detail, string hardwareColor, EquipmentState state, bool isPressed = false)
    {
        Name = name;
        _detail = detail;
        HardwareColor = hardwareColor;
        _state = state;
        _isPressed = isPressed;
    }

    public string Name { get; }

    // 실제 조작 스위치 박스의 물리 버튼 색상입니다. UI 표시용이며 제어 출력값이 아닙니다.
    public string HardwareColor { get; }

    public string Detail
    {
        get => _detail;
        set => SetProperty(ref _detail, value);
    }

    public bool IsPressed
    {
        get => _isPressed;
        set => SetProperty(ref _isPressed, value);
    }

    public EquipmentState State
    {
        get => _state;
        set => SetProperty(ref _state, value);
    }
}
