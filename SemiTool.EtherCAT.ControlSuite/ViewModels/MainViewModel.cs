using System.Collections.ObjectModel;
using System.Windows.Input;
using SemiTool.EtherCAT.ControlSuite.Models;
using SemiTool.EtherCAT.ControlSuite.Services;

namespace SemiTool.EtherCAT.ControlSuite.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly ITeachingValueProvider _teachingValueProvider;
    private readonly OfflineEquipmentSimulator _offlineSimulator;
    private readonly SafetyInterlockEvaluator _interlockEvaluator;
    private readonly CommandGate _commandGate;
    private readonly CommandAuditLog _commandAuditLog;
    private EquipmentSnapshot _snapshot;
    private string _operationMode = "MANUAL";
    private string _selectedRoute = "FOUP A -> CHAMBER A";
    private string _operatorMessage = "실장비 EtherCAT 어댑터 연결 대기";
    private string _connectionMode = "OFFLINE";
    private string _motionPhase = "전원 투입 대기";
    private string _motionPermissionText = "실제 이동 명령 금지 - 승인된 티칭값 소스 미연결";
    private string _offlineSimulationStatus = "오프라인 시뮬레이터 준비 전";
    private int _sequenceProgress;
    private int _simulatorCycle;
    private EquipmentState _equipmentState = EquipmentState.Ready;

    public MainViewModel()
        : this(new ReadOnlyTeachingValueProvider(), new OfflineEquipmentSimulator(), new SafetyInterlockEvaluator())
    {
    }

    public MainViewModel(
        ITeachingValueProvider teachingValueProvider,
        OfflineEquipmentSimulator offlineSimulator,
        SafetyInterlockEvaluator interlockEvaluator)
    {
        _teachingValueProvider = teachingValueProvider;
        _offlineSimulator = offlineSimulator;
        _interlockEvaluator = interlockEvaluator;
        _commandGate = new CommandGate(_interlockEvaluator);
        _commandAuditLog = new CommandAuditLog();
        _snapshot = _offlineSimulator.CreatePowerOnSnapshot();

        // 전면 제어반의 실제 전원/입출력 블록 상태입니다. 이후 PLC/EtherCAT 실신호와 매핑합니다.
        StatusItems = new ObservableCollection<StatusItemViewModel>
        {
            new("AC MAIN POWER", "READY", "전면 제어반 AC 메인 전원", EquipmentState.Ready),
            new("SERVO POWER", "WAIT", "LS 서보 드라이브 준비 신호 대기", EquipmentState.Warning),
            new("DC/PLC POWER", "READY", "PLC 및 DC 전원 라인", EquipmentState.Ready),
            new("EtherCAT I/O", "OFFLINE", "I/O 커플러 및 터미널 연결 대기", EquipmentState.Warning),
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

        // 중앙 이송부 축 상태입니다. 위치값은 실장비 엔코더/드라이브 값을 받기 전까지 실제 좌표로 표시하지 않습니다.
        AxisStatuses = new ObservableCollection<AxisStatusViewModel>
        {
            new("Theta 회전 이송 베이스", "LIVE N/A", "WAIT", "NOT HOMED", "CLEAR", EquipmentState.Warning),
            new("Linear 이송 액추에이터", "LIVE N/A", "WAIT", "NOT HOMED", "CLEAR", EquipmentState.Warning),
            new("Z/Lift 챔버 스테이지", "LIVE N/A", "WAIT", "NOT HOMED", "CLEAR", EquipmentState.Warning),
            new("Vacuum Pickup Head", "SENSOR N/A", "WAIT", "CHECK REQUIRED", "CLEAR", EquipmentState.Warning)
        };

        // 조작 스위치 박스 입력입니다. 실제 출력 명령이 아니라 UI 상태 표시와 인터록 확인용입니다.
        SwitchInputs = new ObservableCollection<SwitchInputViewModel>
        {
            new("Select SW", "조작 스위치 박스 선택 스위치", "Black", EquipmentState.Ready),
            new("Push SW-1", "적재/확인용 푸시 버튼", "Red", EquipmentState.Ready),
            new("Push SW-2", "배출/확인용 푸시 버튼", "Green", EquipmentState.Ready),
            new("EMG SW", "비상정지 스위치", "EmergencyRed", EquipmentState.Ready)
        };

        // 웨이퍼 이송 시퀀스 진행도입니다. 실장비 명령 없이도 경로 조건을 반복 검증할 수 있게 분리했습니다.
        SequenceSteps = new ObservableCollection<SequenceStepViewModel>
        {
            new(1, "FOUP 슬롯맵 확인", "대기", EquipmentState.Ready),
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
        SlotMap = new ObservableCollection<WaferSlotViewModel>();
        Interlocks = new ObservableCollection<InterlockStatusViewModel>();
        BladePose = new BladePoseViewModel();
        CommandAudits = _commandAuditLog.Records;
        TransferStations = new ObservableCollection<TransferStationViewModel>
        {
            new("FOUP A", TransferStationKind.Foup, "5단 슬롯 카세트", EquipmentState.Warning),
            new("FOUP B", TransferStationKind.Foup, "5단 슬롯 카세트", EquipmentState.Warning),
            new("CHAMBER A", TransferStationKind.Chamber, "전면 슬롯 도어", EquipmentState.Ready),
            new("CHAMBER B", TransferStationKind.Chamber, "전면 슬롯 도어", EquipmentState.Ready),
            new("CHAMBER C", TransferStationKind.Chamber, "전면 슬롯 도어", EquipmentState.Ready),
            new("ROBOT HOME", TransferStationKind.RobotHome, "원점 기준", EquipmentState.Warning)
        };

        TeachingStatus = TeachingPoints.Count == 0
            ? "승인된 티칭 데이터 연결 대기 - 임의 좌표 없음"
            : $"{TeachingPoints.Count}개 승인 티칭 데이터 읽기 전용";

        StartCycleCommand = new RelayCommand(_ => StartCycle());
        HoldCommand = new RelayCommand(_ => HoldCycle());
        ResetCommand = new RelayCommand(_ => ResetFaults());
        ServoReadyCommand = new RelayCommand(_ => ConnectOfflineSimulator());
        HomeRequestCommand = new RelayCommand(_ => RequestHomeCheck());
        ToggleSwitchCommand = new RelayCommand(ToggleSwitch);
        SelectRouteCommand = new RelayCommand(SelectRoute);
        ConnectOfflineSimulatorCommand = new RelayCommand(_ => ConnectOfflineSimulator());
        VerifySlotMapCommand = new RelayCommand(_ => VerifySlotMap());
        AdvanceSimulationCommand = new RelayCommand(_ => AdvanceSimulation());
        EmergencyStopCommand = new RelayCommand(_ => TriggerEmergencyStop());
        ToggleChamberDoorCommand = new RelayCommand(ToggleChamberDoor);

        ApplySnapshot(_snapshot, addEvent: false);
        UpdateRouteVisualState();
    }

    public ObservableCollection<StatusItemViewModel> StatusItems { get; }

    public ObservableCollection<StationViewModel> Chambers { get; }

    public ObservableCollection<StationViewModel> Foups { get; }

    public ObservableCollection<AxisStatusViewModel> AxisStatuses { get; }

    public ObservableCollection<SwitchInputViewModel> SwitchInputs { get; }

    public ObservableCollection<SequenceStepViewModel> SequenceSteps { get; }

    public ObservableCollection<AlarmEventViewModel> AlarmEvents { get; }

    public ObservableCollection<TeachingPoint> TeachingPoints { get; }

    public ObservableCollection<WaferSlotViewModel> SlotMap { get; }

    public ObservableCollection<InterlockStatusViewModel> Interlocks { get; }

    public ObservableCollection<TransferStationViewModel> TransferStations { get; }

    public BladePoseViewModel BladePose { get; }

    public ReadOnlyObservableCollection<CommandAuditRecord> CommandAudits { get; }

    public string TeachingStatus { get; }

    public ICommand StartCycleCommand { get; }

    public ICommand HoldCommand { get; }

    public ICommand ResetCommand { get; }

    public ICommand ServoReadyCommand { get; }

    public ICommand HomeRequestCommand { get; }

    public ICommand ToggleSwitchCommand { get; }

    public ICommand SelectRouteCommand { get; }

    public ICommand ConnectOfflineSimulatorCommand { get; }

    public ICommand VerifySlotMapCommand { get; }

    public ICommand AdvanceSimulationCommand { get; }

    public ICommand EmergencyStopCommand { get; }

    public ICommand ToggleChamberDoorCommand { get; }

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

    public string ConnectionMode
    {
        get => _connectionMode;
        set => SetProperty(ref _connectionMode, value);
    }

    public string MotionPhase
    {
        get => _motionPhase;
        set => SetProperty(ref _motionPhase, value);
    }

    public string MotionPermissionText
    {
        get => _motionPermissionText;
        set => SetProperty(ref _motionPermissionText, value);
    }

    public string OfflineSimulationStatus
    {
        get => _offlineSimulationStatus;
        set => SetProperty(ref _offlineSimulationStatus, value);
    }

    public int SequenceProgress
    {
        get => _sequenceProgress;
        set => SetProperty(ref _sequenceProgress, value);
    }

    public int SimulatorCycle
    {
        get => _simulatorCycle;
        set => SetProperty(ref _simulatorCycle, value);
    }

    public EquipmentState EquipmentState
    {
        get => _equipmentState;
        set => SetProperty(ref _equipmentState, value);
    }

    private bool ApprovedTeachingLoaded => TeachingPoints.Count > 0;

    private void StartCycle()
    {
        var permission = _interlockEvaluator.GetMotionPermission(_snapshot, ApprovedTeachingLoaded);

        if (!permission.CanRunOfflineSimulation)
        {
            EquipmentState = EquipmentState.Warning;
            OperatorMessage = $"시작 조건 미충족: {permission.Reason}";
            RaiseAlarm("INTERLOCK", permission.Reason, EquipmentState.Warning);
            UpdateInterlocks();
            return;
        }

        AdvanceSimulation();
    }

    private void HoldCycle()
    {
        OperationMode = "HOLD";
        EquipmentState = EquipmentState.Warning;
        SequenceProgress = Math.Min(SequenceProgress, 50);
        OfflineSimulationStatus = "작업자 홀드 - 시뮬레이션 진행 정지";
        OperatorMessage = "작업자 홀드 요청 - 구동 명령 출력 전 상태 유지";
        RaiseAlarm("OPERATOR", "작업자 홀드 상태로 전환했습니다.", EquipmentState.Warning);
    }

    private void ResetFaults()
    {
        _snapshot = _offlineSimulator.CreatePowerOnSnapshot();
        OperationMode = "MANUAL";
        ConnectionMode = "OFFLINE";
        OperatorMessage = "리셋 완료 - 오프라인 시뮬레이터 또는 실장비 어댑터 연결 필요";

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

        ApplySnapshot(_snapshot);
        RaiseAlarm("SYSTEM", "알람 상태와 오프라인 시뮬레이터 상태를 초기화했습니다.", EquipmentState.Ready);
    }

    private void ConnectOfflineSimulator()
    {
        AppendCommandAudit(EquipmentCommandType.CheckConnection, allowedFallback: true);
        _snapshot = _offlineSimulator.ConnectOfflineRig();
        ConnectionMode = "OFFLINE SIM";
        OperationMode = "SIM READY";
        OperatorMessage = "집 개발용 오프라인 시뮬레이터 연결 완료 - 실제 출력 명령 없음";
        ApplySnapshot(_snapshot);
        RaiseAlarm("SIM", "EtherCAT 실장비 없이 인터록/시퀀스 검증용 시뮬레이터를 연결했습니다.", EquipmentState.Ready);
    }

    private void VerifySlotMap()
    {
        AppendCommandAudit(EquipmentCommandType.ReadSlotMap, allowedFallback: _snapshot.EtherCatLink);
        _snapshot = _offlineSimulator.VerifySlotMap();
        OperatorMessage = "FOUP 5단 슬롯맵 검증 완료 - SIM 식별자는 실제 웨이퍼 ID가 아닙니다.";
        ApplySnapshot(_snapshot);
        RaiseAlarm("FOUP", "오프라인 슬롯맵을 검증 완료 상태로 갱신했습니다.", EquipmentState.Ready);
    }

    private void AdvanceSimulation()
    {
        var decision = AppendCommandAudit(EquipmentCommandType.AdvanceOfflineSimulation);

        if (!decision.IsAllowed)
        {
            OperatorMessage = decision.Reason;
            RaiseAlarm("COMMAND", decision.Reason, EquipmentState.Warning);
            UpdateInterlocks();
            return;
        }

        _snapshot = _offlineSimulator.AdvanceCycle(SelectedRoute);
        OperationMode = _snapshot.SequenceProgress >= 100 ? "SIM COMPLETE" : "AUTO CHECK";
        OperatorMessage = _snapshot.MotionPhase;
        ApplySnapshot(_snapshot);
        RaiseAlarm("SEQUENCE", _snapshot.MotionPhase, _snapshot.SequenceProgress >= 100 ? EquipmentState.Ready : EquipmentState.Active);
    }

    private void RequestHomeCheck()
    {
        AppendCommandAudit(EquipmentCommandType.CheckHome, allowedFallback: true);

        foreach (var axis in AxisStatuses)
        {
            axis.Home = _snapshot.AxisHomed ? "HOME OK" : "CHECK REQUESTED";
            axis.State = _snapshot.AxisHomed ? EquipmentState.Ready : EquipmentState.Warning;
        }

        OperatorMessage = "홈 확인 요청 - 실제 홈 완료 신호 수신 전까지 티칭값 이동 금지";
        RaiseAlarm("MOTION", "홈 확인 요청 상태입니다. 임의 티칭 이동값은 사용하지 않습니다.", EquipmentState.Warning);
        UpdateInterlocks();
    }

    private void TriggerEmergencyStop()
    {
        AppendCommandAudit(EquipmentCommandType.StopMotion, allowedFallback: true);
        var emgSwitch = SwitchInputs.First(input => input.Name == "EMG SW");
        emgSwitch.IsPressed = true;
        emgSwitch.State = EquipmentState.Fault;
        _snapshot = _offlineSimulator.SetEmergencyStop(true);
        OperationMode = "EMERGENCY STOP";
        OperatorMessage = "비상정지 테스트 입력 활성 - 모든 이송 동작 금지";
        ApplySnapshot(_snapshot);
        RaiseAlarm("SAFETY", "비상정지 테스트를 실행했습니다.", EquipmentState.Fault);
    }

    private void ToggleChamberDoor(object? parameter)
    {
        if (parameter is not StationViewModel chamber || !chamber.Name.StartsWith("CHAMBER", StringComparison.Ordinal))
        {
            return;
        }

        chamber.IsDoorOpen = !chamber.IsDoorOpen;
        chamber.PrimaryStatus = chamber.IsDoorOpen ? "Door Open" : "Door Closed";
        chamber.InterlockStatus = chamber.IsDoorOpen ? "Door Interlock Blocked" : "Door Interlock Ready";
        chamber.State = chamber.IsDoorOpen ? EquipmentState.Fault : EquipmentState.Ready;

        var transferStation = TransferStations.FirstOrDefault(station => station.Name == chamber.Name);
        if (transferStation is not null)
        {
            transferStation.IsDoorOpen = chamber.IsDoorOpen;
            transferStation.State = chamber.IsDoorOpen ? EquipmentState.Fault : EquipmentState.Ready;
            transferStation.Status = chamber.IsDoorOpen ? "도어 열림 - 이송 금지" : "도어 닫힘";
        }

        var anyDoorOpen = Chambers.Any(item => item.IsDoorOpen);
        _snapshot = _offlineSimulator.SetChamberDoorOpen(anyDoorOpen);
        OperatorMessage = $"{chamber.Name} {(chamber.IsDoorOpen ? "도어 열림" : "도어 닫힘")} - 도어 인터록 갱신";
        ApplySnapshot(_snapshot);
        RaiseAlarm("CHAMBER", OperatorMessage, chamber.State);
    }

    private void ToggleSwitch(object? parameter)
    {
        if (parameter is not SwitchInputViewModel input)
        {
            return;
        }

        input.IsPressed = !input.IsPressed;

        if (input.Name == "EMG SW")
        {
            _snapshot = _offlineSimulator.SetEmergencyStop(input.IsPressed);
            input.State = input.IsPressed ? EquipmentState.Fault : EquipmentState.Ready;
            OperationMode = input.IsPressed ? "EMERGENCY STOP" : "MANUAL";
            OperatorMessage = input.IsPressed ? "비상정지 입력 활성 - 모든 이송 동작 금지" : "비상정지 해제 - 인터록 재확인 필요";
            ApplySnapshot(_snapshot);
            RaiseAlarm("SAFETY", input.IsPressed ? "비상정지 스위치 입력이 활성화되었습니다." : "비상정지 입력이 해제되었습니다.", input.State);
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
        UpdateRouteVisualState();
        RaiseAlarm("ROUTE", $"{route} 경로를 선택했습니다.", EquipmentState.Ready);
    }

    private void ApplySnapshot(EquipmentSnapshot snapshot, bool addEvent = true)
    {
        SimulatorCycle = snapshot.Cycle;
        SequenceProgress = snapshot.SequenceProgress;
        MotionPhase = snapshot.MotionPhase;
        OfflineSimulationStatus = snapshot.EtherCatLink ? "오프라인 시뮬레이터 연결됨" : "오프라인 시뮬레이터 미연결";

        var permission = _interlockEvaluator.GetMotionPermission(snapshot, ApprovedTeachingLoaded);
        MotionPermissionText = permission.Reason;
        EquipmentState = snapshot.EmergencyStop
            ? EquipmentState.Fault
            : snapshot.SequenceProgress is > 0 and < 100
                ? EquipmentState.Active
                : permission.CanRunOfflineSimulation ? EquipmentState.Ready : EquipmentState.Warning;

        UpdateStatus("EtherCAT I/O", snapshot.EtherCatLink ? "SIM LINK" : "OFFLINE", "I/O 커플러 연결 상태 또는 오프라인 링크", snapshot.EtherCatLink ? EquipmentState.Ready : EquipmentState.Warning);
        UpdateStatus("SERVO POWER", snapshot.ServoReady ? "ON READY" : "WAIT", "서보 드라이브 준비 신호", snapshot.ServoReady ? EquipmentState.Ready : EquipmentState.Warning);
        UpdateStatus("SYSTEM I/O", snapshot.EmergencyStop ? "EMG ACTIVE" : "READY", "센서, 버튼, 타워 라이트 입출력", snapshot.EmergencyStop ? EquipmentState.Fault : EquipmentState.Ready);

        foreach (var axis in AxisStatuses)
        {
            axis.Position = snapshot.EtherCatLink ? "SIM FEEDBACK" : "LIVE N/A";
            axis.Servo = snapshot.ServoReady ? "ON READY" : "WAIT";
            axis.Home = snapshot.AxisHomed ? "HOME OK" : "NOT HOMED";
            axis.Limit = snapshot.RouteClear ? "CLEAR" : "BLOCKED";
            axis.State = snapshot.EmergencyStop ? EquipmentState.Fault : snapshot.AxisHomed ? EquipmentState.Ready : EquipmentState.Warning;
        }

        foreach (var foup in Foups)
        {
            foup.PrimaryStatus = snapshot.FoupCassettePresent ? "Cassette Present" : "Cassette Missing";
            foup.SecondaryStatus = snapshot.SlotMapVerified ? "Slot Map Verified" : "Slot Map Pending";
            foup.State = snapshot.SlotMapVerified ? EquipmentState.Ready : EquipmentState.Warning;
        }

        UpdateSequenceSteps(snapshot.SequenceProgress);
        UpdateSlotMap(snapshot.SlotMap);
        UpdateInterlocks();
        UpdateBladePose(snapshot);
        UpdateTransferStationStatus(snapshot);

        if (addEvent)
        {
            OnPropertyChanged(nameof(MotionPermissionText));
        }
    }

    private void UpdateSequenceSteps(int progress)
    {
        var statuses = progress switch
        {
            >= 100 => new[] { "완료", "완료", "완료", "완료", "완료" },
            >= 86 => new[] { "완료", "완료", "완료", "확인 중", "대기" },
            >= 64 => new[] { "완료", "완료", "확인 중", "대기", "대기" },
            >= 42 => new[] { "완료", "확인 중", "대기", "대기", "대기" },
            >= 20 => new[] { "확인 중", "대기", "대기", "대기", "대기" },
            _ => new[] { "대기", "대기", "대기", "대기", "대기" }
        };

        for (var index = 0; index < SequenceSteps.Count; index++)
        {
            SequenceSteps[index].Status = statuses[index];
            SequenceSteps[index].State = statuses[index] switch
            {
                "완료" => EquipmentState.Ready,
                "확인 중" => EquipmentState.Active,
                _ => EquipmentState.Ready
            };
        }
    }

    private void UpdateSlotMap(IReadOnlyList<WaferSlotSnapshot> snapshots)
    {
        SlotMap.Clear();

        foreach (var snapshot in snapshots)
        {
            SlotMap.Add(new WaferSlotViewModel(snapshot));
        }
    }

    private void UpdateInterlocks()
    {
        Interlocks.Clear();

        foreach (var check in _interlockEvaluator.Evaluate(_snapshot, ApprovedTeachingLoaded))
        {
            Interlocks.Add(new InterlockStatusViewModel(check));
        }
    }

    private void UpdateRouteVisualState()
    {
        var (source, target) = ParseSelectedRoute();

        foreach (var station in TransferStations)
        {
            station.IsSource = station.Name == source;
            station.IsTarget = station.Name == target;
        }

        BladePose.Target = target;
        BladePose.Direction = target switch
        {
            "CHAMBER A" => "북쪽 챔버 A 방향",
            "CHAMBER B" => "좌측 챔버 B 방향",
            "CHAMBER C" => "우측 챔버 C 방향",
            _ when source.StartsWith("FOUP A", StringComparison.Ordinal) => "좌측 FOUP A 방향",
            _ when source.StartsWith("FOUP B", StringComparison.Ordinal) => "우측 FOUP B 방향",
            _ => "HOME"
        };
        BladePose.VisualAngle = target switch
        {
            "CHAMBER A" => 0,
            "CHAMBER B" => -72,
            "CHAMBER C" => 72,
            _ when source.StartsWith("FOUP A", StringComparison.Ordinal) => -128,
            _ when source.StartsWith("FOUP B", StringComparison.Ordinal) => 128,
            _ => 0
        };
    }

    private void UpdateBladePose(EquipmentSnapshot snapshot)
    {
        var (_, target) = ParseSelectedRoute();
        BladePose.Target = target;
        BladePose.Phase = snapshot.MotionPhase;
        BladePose.Reach = snapshot.SequenceProgress switch
        {
            >= 86 => "Extend to chamber slot",
            >= 64 => "Vacuum pickup hold",
            >= 42 => "Extend to source slot",
            >= 20 => "Rotate to source",
            _ => "Retracted at home"
        };
        BladePose.BladeLength = snapshot.SequenceProgress switch
        {
            >= 86 => 185,
            >= 64 => 160,
            >= 42 => 176,
            >= 20 => 126,
            _ => 118
        };
        BladePose.State = snapshot.EmergencyStop
            ? EquipmentState.Fault
            : snapshot.SequenceProgress > 0 ? EquipmentState.Active : EquipmentState.Warning;

        UpdateRouteVisualState();
    }

    private void UpdateTransferStationStatus(EquipmentSnapshot snapshot)
    {
        foreach (var station in TransferStations)
        {
            if (station.Kind == TransferStationKind.Foup)
            {
                station.Status = snapshot.SlotMapVerified ? "슬롯맵 검증 완료" : "슬롯맵 대기";
                station.State = snapshot.SlotMapVerified ? EquipmentState.Ready : EquipmentState.Warning;
            }

            if (station.Kind == TransferStationKind.RobotHome)
            {
                station.Status = snapshot.AxisHomed ? "홈 완료" : "홈 확인 대기";
                station.State = snapshot.AxisHomed ? EquipmentState.Ready : EquipmentState.Warning;
            }
        }
    }

    private (string Source, string Target) ParseSelectedRoute()
    {
        var parts = SelectedRoute.Split(" -> ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        return parts.Length == 2 ? (parts[0], parts[1]) : ("ROBOT HOME", "ROBOT HOME");
    }

    private void UpdateStatus(string name, string value, string detail, EquipmentState state)
    {
        var item = StatusItems.First(status => status.Name == name);
        item.Value = value;
        item.Detail = detail;
        item.State = state;
    }

    private CommandDecision AppendCommandAudit(EquipmentCommandType commandType, bool? allowedFallback = null)
    {
        var command = EquipmentCommand.Create(commandType, SelectedRoute, "UI");
        var decision = _commandGate.Evaluate(command, _snapshot, ApprovedTeachingLoaded);

        if (allowedFallback is not null)
        {
            decision = decision with { IsAllowed = allowedFallback.Value };
        }

        _commandAuditLog.Append(decision);
        return decision;
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
