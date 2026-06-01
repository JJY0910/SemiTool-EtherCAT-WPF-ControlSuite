using SemiTool.EtherCAT.ControlSuite.Models;

namespace SemiTool.EtherCAT.ControlSuite.ViewModels;

public sealed class AxisStatusViewModel : ObservableObject
{
    private EquipmentState _state;
    private string _position;
    private string _servo;
    private string _home;
    private string _limit;

    public AxisStatusViewModel(string name, string position, string servo, string home, string limit, EquipmentState state)
    {
        Name = name;
        _position = position;
        _servo = servo;
        _home = home;
        _limit = limit;
        _state = state;
    }

    public string Name { get; }

    // 실제 드라이브/엔코더에서 읽은 현재 위치 표시값입니다. 승인 전에는 임의 수치를 넣지 않습니다.
    public string Position
    {
        get => _position;
        set => SetProperty(ref _position, value);
    }

    // 서보 전원 및 준비 상태입니다. UI 버튼은 표시 상태만 바꾸며 실제 인에이블 출력은 별도 어댑터에서 처리합니다.
    public string Servo
    {
        get => _servo;
        set => SetProperty(ref _servo, value);
    }

    // 원점 확인 상태입니다. 홈 완료 신호가 들어오기 전까지 티칭 위치 이동을 막는 기준입니다.
    public string Home
    {
        get => _home;
        set => SetProperty(ref _home, value);
    }

    // 소프트/하드 리미트 감지 상태입니다.
    public string Limit
    {
        get => _limit;
        set => SetProperty(ref _limit, value);
    }

    public EquipmentState State
    {
        get => _state;
        set => SetProperty(ref _state, value);
    }
}
