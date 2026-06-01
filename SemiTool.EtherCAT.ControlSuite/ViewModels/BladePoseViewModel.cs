using SemiTool.EtherCAT.ControlSuite.Models;

namespace SemiTool.EtherCAT.ControlSuite.ViewModels;

public sealed class BladePoseViewModel : ObservableObject
{
    private string _homeReference = "HOME REF: Theta 0 / Linear Retract / Z Safe";
    private string _direction = "HOME";
    private string _reach = "Retracted";
    private string _target = "None";
    private string _phase = "대기";
    private double _visualAngle;
    private double _bladeLength = 118;
    private EquipmentState _state = EquipmentState.Warning;

    // 원점 기준 표시는 티칭값이 아니라 화면 기준입니다. 실제 좌표값은 승인 소스 연결 후 별도 표시합니다.
    public string HomeReference
    {
        get => _homeReference;
        set => SetProperty(ref _homeReference, value);
    }

    // 블레이드가 바라보는 방향입니다. 실제 축 각도가 아니라 선택 경로와 시뮬레이터 단계 기반 표시입니다.
    public string Direction
    {
        get => _direction;
        set => SetProperty(ref _direction, value);
    }

    // 블레이드 전진/후퇴 상태입니다.
    public string Reach
    {
        get => _reach;
        set => SetProperty(ref _reach, value);
    }

    public string Target
    {
        get => _target;
        set => SetProperty(ref _target, value);
    }

    public string Phase
    {
        get => _phase;
        set => SetProperty(ref _phase, value);
    }

    // 화면 상면도에서 블레이드가 회전해 보이는 각도입니다. 실제 모터 각도나 티칭값이 아닙니다.
    public double VisualAngle
    {
        get => _visualAngle;
        set => SetProperty(ref _visualAngle, value);
    }

    // 화면 상면도에서 블레이드 전진/후퇴를 보여주는 길이입니다. 실제 리니어 축 위치가 아닙니다.
    public double BladeLength
    {
        get => _bladeLength;
        set => SetProperty(ref _bladeLength, value);
    }

    public EquipmentState State
    {
        get => _state;
        set => SetProperty(ref _state, value);
    }
}
