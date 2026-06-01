using SemiTool.EtherCAT.ControlSuite.Models;

namespace SemiTool.EtherCAT.ControlSuite.ViewModels;

public sealed class BladePoseViewModel : ObservableObject
{
    private string _homeReference = "HOME: theta 0 deg / blade retracted / Z safe";
    private string _direction = "HOME";
    private string _reach = "Retracted";
    private string _source = "FOUP A";
    private string _target = "CHAMBER A";
    private string _currentStation = "HOME";
    private string _phase = "Home ready";
    private string _vacuum = "Vacuum OFF";
    private string _wafer = "No wafer on blade";
    private double _visualAngle;
    private double _bladeLength = 86;
    private bool _vacuumOn;
    private bool _waferOnBlade;
    private TransferPhase _transferPhase = TransferPhase.HomeReady;
    private EquipmentState _state = EquipmentState.Warning;

    public string HomeReference
    {
        get => _homeReference;
        set => SetProperty(ref _homeReference, value);
    }

    public string Direction
    {
        get => _direction;
        set => SetProperty(ref _direction, value);
    }

    public string Reach
    {
        get => _reach;
        set => SetProperty(ref _reach, value);
    }

    public string Source
    {
        get => _source;
        set => SetProperty(ref _source, value);
    }

    public string Target
    {
        get => _target;
        set => SetProperty(ref _target, value);
    }

    public string CurrentStation
    {
        get => _currentStation;
        set => SetProperty(ref _currentStation, value);
    }

    public string Phase
    {
        get => _phase;
        set => SetProperty(ref _phase, value);
    }

    public string Vacuum
    {
        get => _vacuum;
        set => SetProperty(ref _vacuum, value);
    }

    public string Wafer
    {
        get => _wafer;
        set => SetProperty(ref _wafer, value);
    }

    // 3D 화면용 회전 각도입니다. 실제 모터 티칭값을 저장하거나 덮어쓰지 않습니다.
    public double VisualAngle
    {
        get => _visualAngle;
        set => SetProperty(ref _visualAngle, value);
    }

    // 3D 화면용 블레이드 전진 길이입니다. 실제 직선축 위치값이 아닙니다.
    public double BladeLength
    {
        get => _bladeLength;
        set => SetProperty(ref _bladeLength, value);
    }

    public bool VacuumOn
    {
        get => _vacuumOn;
        set => SetProperty(ref _vacuumOn, value);
    }

    public bool WaferOnBlade
    {
        get => _waferOnBlade;
        set => SetProperty(ref _waferOnBlade, value);
    }

    public TransferPhase TransferPhase
    {
        get => _transferPhase;
        set => SetProperty(ref _transferPhase, value);
    }

    public EquipmentState State
    {
        get => _state;
        set => SetProperty(ref _state, value);
    }
}
