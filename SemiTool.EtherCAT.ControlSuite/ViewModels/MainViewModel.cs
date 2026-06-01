using System.Collections.ObjectModel;
using System.Windows.Input;
using SemiTool.EtherCAT.ControlSuite.Models;
using SemiTool.EtherCAT.ControlSuite.Services;

namespace SemiTool.EtherCAT.ControlSuite.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly ITeachingValueProvider _teachingValueProvider;
    private string _operationMode = "MANUAL";
    private string _selectedRoute = "FOUP A -> CHAMBER A";
    private string _operatorMessage = "실장비 EtherCAT 어댑터 연결 대기";
    private int _sequenceProgress;
    private EquipmentState _equipmentState = EquipmentState.Ready;

    public MainViewModel()
        : this(new ReadOnlyTeachingValueProvider())
    {
    }

    public MainViewModel(ITeachingValueProvider teachingValueProvider)
    {
        _teachingValueProvider = teachingValueProvider;

        // 전면 제어반의 실제 전원/입출력 블록 상태입니다. 이후 PLC/EtherCAT 실신호와 매핑합니다.
        StatusItems = new ObservableCollection<StatusItemViewModel>
        {
            new("AC MAIN POWER", "READY", "전면 제어반 AC 메인 전원", EquipmentState.Ready),
            new("SERVO POWER", "READY", "LS 서보 드라이브 전원 확인 대기", EquipmentState.Ready),
            new("DC/PLC POWER", "READY", "PLC 및 DC 전원 라인", EquipmentState.Ready),
            new("EtherCAT I/O", "STANDBY", "I/O 커플러 및 터미널 연결 대기", EquipmentState.Warning),
            new("SYSTEM I/O", "READY", "센서, 버튼, 타워 라이트 입출력", EquipmentState.Ready),
            new("24V SUPPLY", "READY", "Mean Well 24V 전원공급기", EquipmentState.Ready)
        };

        // 장비 사진 기준 챔버 A/B/C의 도어, 스테이지, 인터록 표시 영역입니다.
        Chambers = new ObservableCollection<StationViewModel>
        {
            new("CHAMBER A", "전면 슬롯 + 웨이퍼 스테이지", "Door Closed", "Stage Ready", "Vacuum Interlock Ready", EquipmentState.Ready),
            new("CHAMBER B", "전면 슬롯 + 내부 공정 공간", "Door Closed", "Stage Ready", "Lift Interlock Ready", EquipmentState.Ready),
            new("CHAMBER C", "전면 슬롯 + 근접 센서", "Door Closed", "Stage Ready", "Presence Interlock Ready", EquipmentState.Ready)
        };

        // FOUP A/B 카세트와 슬롯 맵 상태입니다. 슬롯별 웨이퍼 유무는 실제 센서 연동 후 갱신합니다.
        Foups = new ObservableCollection<StationViewModel>
        {
            new("FOUP A", "5단 웨이퍼 슬롯 카세트", "Cassette Present", "Slot Map Pending", "Position Sensor Ready", EquipmentState.Warning),
            new("FOUP B", "5단 웨이퍼 슬롯 카세트", "Cassette Present", "Slot Map Pending", "Position Sensor Ready", EquipmentState.Warning)
        };

        // 중앙 이송부 축 상태입니다. 위치값은 실장비 엔코더/드라이브 값을 받기 전까지 LIVE N/A로 둡니다.
        AxisStatuses = new ObservableCollection<AxisStatusViewModel>
        {
            new("Theta 회전 이송 베이스", "LIVE N/A", "READY", "NOT HOMED", "CLEAR", EquipmentState.Warning),
            new("Linear 이송 액추에이터", "LIVE N/A", "READY", "NOT HOMED", "CLEAR", EquipmentState.Warning),
            new("Z/Lift 챔버 스테이지", "LIVE N/A", "READY", "NOT HOMED", "CLEAR", EquipmentState.Warning),
            new("Vacuum Pickup Head", "SENSOR N/A", "READY", "CHECK REQUIRED", "CLEAR", EquipmentState.Warning)
        };

        // 조작 스위치 박스 입력입니다. 실제 출력 명령이 아니라 UI 상태 표시와 인터록 확인용입니다.
        SwitchInputs = new ObservableCollection<SwitchInputViewModel>
        {
            new("Select SW", "조작 스위치 박스 선택 스위치", "Black", EquipmentState.Ready),
            new("Push SW-1", "적재/확인용 푸시 버튼", "Red", EquipmentState.Ready),
            new("Push SW-2", "배출/확인용 푸시 버튼", "Green", EquipmentState.Ready),
            new("EMG SW", "비상정지 스위치", "EmergencyRed", EquipmentState.Ready)
        };

        // 웨이퍼 이송 시퀀스 진행도입니다. 실제 장비 명령 전에는 조건 확인 단계까지만 표시합니다.
        SequenceSteps = new ObservableCollection<SequenceStepViewModel>
        {
            new(1, "FOUP 슬롯 맵 확인", "대기", EquipmentState.Ready),
            new(2, "챔버 도어 및 스테이지 인터록", "대기", EquipmentState.Ready),
            new(3, "픽업 헤드 진공 확인", "대기", EquipmentState.Ready),
            new(4, "회전/선형 이송 경로 확인", "대기", EquipmentState.Ready),
            new(5, "챔버 적재 완료 확인", "대기", EquipmentState.Ready)
        };

        // 운영자가 즉시 확인할 수 있는 최근 이벤트 로그입니다.
        AlarmEvents = new ObservableCollection<AlarmEventViewModel>
        {
            new(DateTimeOffset.Now, "SYSTEM", "초기 UI 셸 로드 완료. 실장비 연결은 별도 어댑터 필요.", EquipmentState.Ready),
            new(DateTimeOffset.Now, "TEACHING", "티칭값은 승인된 소스에서만 읽으며 UI에서 수정하지 않습니다.", EquipmentState.Warning)
        };

        // 승인된 티칭값은 읽기 전용으로만 표시합니다. 여기서 임의 티칭 데이터를 만들지 않습니다.
        TeachingPoints = new ObservableCollection<TeachingPoint>(_teachingValueProvider.LoadApprovedTeachingPoints());
        TeachingStatus = TeachingPoints.Count == 0
            ? "승인된 티칭 데이터 연결 대기 - 임의 좌표 없음"
            : $"{TeachingPoints.Count}개 승인 티칭 데이터 읽기 전용";

        StartCycleCommand = new RelayCommand(_ => StartCycle());
        HoldCommand = new RelayCommand(_ => HoldCycle());
        ResetCommand = new RelayCommand(_ => ResetFaults());
        ServoReadyCommand = new RelayCommand(_ => MarkServoReady());
        HomeRequestCommand = new RelayCommand(_ => RequestHomeCheck());
        ToggleSwitchCommand = new RelayCommand(ToggleSwitch);
        SelectRouteCommand = new RelayCommand(SelectRoute);
    }

    public ObservableCollection<StatusItemViewModel> StatusItems { get; }

    public ObservableCollection<StationViewModel> Chambers { get; }

    public ObservableCollection<StationViewModel> Foups { get; }

    public ObservableCollection<AxisStatusViewModel> AxisStatuses { get; }

    public ObservableCollection<SwitchInputViewModel> SwitchInputs { get; }

    public ObservableCollection<SequenceStepViewModel> SequenceSteps { get; }

    public ObservableCollection<AlarmEventViewModel> AlarmEvents { get; }

    public ObservableCollection<TeachingPoint> TeachingPoints { get; }

    public string TeachingStatus { get; }

    public ICommand StartCycleCommand { get; }

    public ICommand HoldCommand { get; }

    public ICommand ResetCommand { get; }

    public ICommand ServoReadyCommand { get; }

    public ICommand HomeRequestCommand { get; }

    public ICommand ToggleSwitchCommand { get; }

    public ICommand SelectRouteCommand { get; }

    public string OperationMode
    {
        get => _operationMode;
        set => SetProperty(ref _operationMode, value);
    }

    public string SelectedRoute
    {
        get => _selectedRoute;
        set => SetProperty(ref _selectedRoute, value);
    }

    public string OperatorMessage
    {
        get => _operatorMessage;
        set => SetProperty(ref _operatorMessage, value);
    }

    public int SequenceProgress
    {
        get => _sequenceProgress;
        set => SetProperty(ref _sequenceProgress, value);
    }

    public EquipmentState EquipmentState
    {
        get => _equipmentState;
        set => SetProperty(ref _equipmentState, value);
    }

    private void StartCycle()
    {
        if (SwitchInputs.Any(input => input.Name == "EMG SW" && input.IsPressed))
        {
            RaiseAlarm("SAFETY", "비상정지 입력 활성 상태에서는 이송 시퀀스를 시작할 수 없습니다.", EquipmentState.Fault);
            return;
        }

        OperationMode = "AUTO CHECK";
        EquipmentState = EquipmentState.Active;
        SequenceProgress = 34;
        OperatorMessage = $"{SelectedRoute} 경로 인터록 확인 중";

        SequenceSteps[0].Status = "확인 중";
        SequenceSteps[0].State = EquipmentState.Active;
        SequenceSteps[1].Status = "대기";
        SequenceSteps[2].Status = "대기";
        SequenceSteps[3].Status = "대기";
        SequenceSteps[4].Status = "대기";

        StatusItems.First(item => item.Name == "EtherCAT I/O").Value = "LINK CHECK";
        StatusItems.First(item => item.Name == "EtherCAT I/O").State = EquipmentState.Active;
        RaiseAlarm("SEQUENCE", "자동 이송 전 조건 확인을 시작했습니다.", EquipmentState.Active);
    }

    private void HoldCycle()
    {
        OperationMode = "HOLD";
        EquipmentState = EquipmentState.Warning;
        SequenceProgress = Math.Min(SequenceProgress, 50);
        OperatorMessage = "작업자 홀드 요청 - 구동 명령 출력 전 상태 유지";
        RaiseAlarm("OPERATOR", "작업자 홀드 상태로 전환했습니다.", EquipmentState.Warning);
    }

    private void ResetFaults()
    {
        OperationMode = "MANUAL";
        EquipmentState = EquipmentState.Ready;
        SequenceProgress = 0;
        OperatorMessage = "리셋 완료 - 실장비 연결 및 인터록 재확인 필요";

        foreach (var input in SwitchInputs)
        {
            input.IsPressed = false;
            input.State = EquipmentState.Ready;
        }

        foreach (var step in SequenceSteps)
        {
            step.Status = "대기";
            step.State = EquipmentState.Ready;
        }

        foreach (var item in StatusItems)
        {
            item.State = item.Name == "EtherCAT I/O" ? EquipmentState.Warning : EquipmentState.Ready;
            item.Value = item.Name == "EtherCAT I/O" ? "STANDBY" : "READY";
        }

        RaiseAlarm("SYSTEM", "알람 상태를 초기화했습니다.", EquipmentState.Ready);
    }

    private void MarkServoReady()
    {
        foreach (var axis in AxisStatuses)
        {
            axis.Servo = "ON READY";
            axis.State = EquipmentState.Ready;
        }

        StatusItems.First(item => item.Name == "SERVO POWER").Value = "ON READY";
        OperatorMessage = "서보 준비 상태 표시 완료 - 실제 인에이블 출력은 장비 어댑터 연동 후 수행";
        RaiseAlarm("SERVO", "서보 준비 표시를 갱신했습니다.", EquipmentState.Ready);
    }

    private void RequestHomeCheck()
    {
        foreach (var axis in AxisStatuses)
        {
            axis.Home = "CHECK REQUESTED";
            axis.State = EquipmentState.Warning;
        }

        OperatorMessage = "홈 확인 요청 - 실제 홈 완료 신호 수신 전까지 티칭값 이동 금지";
        RaiseAlarm("MOTION", "홈 확인 요청 상태입니다. 임의 티칭 이동값은 사용하지 않습니다.", EquipmentState.Warning);
    }

    private void ToggleSwitch(object? parameter)
    {
        if (parameter is not SwitchInputViewModel input)
        {
            return;
        }

        input.IsPressed = !input.IsPressed;

        if (input.Name == "EMG SW" && input.IsPressed)
        {
            input.State = EquipmentState.Fault;
            EquipmentState = EquipmentState.Fault;
            OperationMode = "EMERGENCY STOP";
            SequenceProgress = 0;
            OperatorMessage = "비상정지 입력 활성 - 모든 이송 동작 금지";
            RaiseAlarm("SAFETY", "비상정지 스위치 입력이 활성화되었습니다.", EquipmentState.Fault);
            return;
        }

        input.State = input.IsPressed ? EquipmentState.Active : EquipmentState.Ready;
        OperatorMessage = $"{input.Name} 입력 {(input.IsPressed ? "ON" : "OFF")}";
        RaiseAlarm("SWITCH", $"{input.Name} 입력 상태가 변경되었습니다.", input.State);
    }

    private void SelectRoute(object? parameter)
    {
        if (parameter is not string route)
        {
            return;
        }

        SelectedRoute = route;
        OperatorMessage = $"{route} 경로 선택 - 티칭 좌표는 승인 소스 연결 전까지 표시하지 않음";
    }

    private void RaiseAlarm(string source, string message, EquipmentState state)
    {
        AlarmEvents.Insert(0, new AlarmEventViewModel(DateTimeOffset.Now, source, message, state));

        while (AlarmEvents.Count > 8)
        {
            AlarmEvents.RemoveAt(AlarmEvents.Count - 1);
        }
    }
}
