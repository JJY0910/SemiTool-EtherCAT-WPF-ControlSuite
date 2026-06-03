using System.Collections.ObjectModel;
using SemiTool.Application;
using SemiTool.Domain;

namespace SemiTool.Hmi.Wpf.ViewModels;

public sealed class MachineTwinViewModel : ObservableObject
{
    private readonly RuntimeCoordinator _runtime;
    private readonly DigitalTwinPhysicalModel _physicalModel;
    private readonly IReadOnlyList<MachineTwinSequenceStep> _sequenceSteps;
    private readonly MachineTwinSequenceStep _resetStep;
    private CancellationTokenSource? _sequenceCts;
    private bool _isSequenceRunning;
    private bool _isSimulatorMode = true;
    private bool _isRealHardwareMode;
    private bool _isConnected;
    private string _machineState = SemiTool.Domain.MachineState.Offline.ToString();
    private string _currentStation = "Home / Start";
    private string _previousStation = "-";
    private string _nextStation = "Chamber A";
    private string _currentStepName = "Simulator startup / safe state";
    private string _currentAction = "Pipeline ready: FOUP A 5 wafers, FOUP B empty";
    private string _operationWafer = "-";
    private string _operationSource = "Home / Start";
    private string _operationDestination = "FOUP A";
    private string _operationCurrentStep = "Ready";
    private string _operationNextStep = "Move theta to FOUP A";
    private string _robotSequenceState = SemiTool.Domain.RobotSequenceState.Idle.ToString();
    private string _bladeSequenceState = SemiTool.Domain.BladeSequenceState.Retracted.ToString();
    private string _vacuumDisplayState = SemiTool.Domain.VacuumSequenceState.Off.ToString();
    private string _chamberADoorState = SemiTool.Domain.ChamberDoorSequenceState.Closed.ToString();
    private string _chamberBDoorState = SemiTool.Domain.ChamberDoorSequenceState.Closed.ToString();
    private string _chamberCDoorState = SemiTool.Domain.ChamberDoorSequenceState.Closed.ToString();
    private double _visualThetaAngle = -180;
    private string _thetaTargetName = "Home / Start";
    private long _preservedThetaEncoderValue;
    private string _zState = "Z Safe";
    private bool _isBladeExtended;
    private bool _isCylinderForward;
    private bool _isCylinderBackward = true;
    private bool _isVacuumOn;
    private bool _isWaferOnBlade;
    private bool _isWaferInFoupA1 = true;
    private bool _isWaferInChamberA;
    private bool _isWaferInChamberB;
    private bool _isWaferInChamberC;
    private bool _isWaferInFoupB1;
    private bool _chamberADoorOpen;
    private bool _chamberBDoorOpen;
    private bool _chamberCDoorOpen;
    private bool _towerRed;
    private bool _towerYellow;
    private bool _towerGreen;
    private string _alarmSummary = "No active alarms";
    private string _selectedSequenceSpeed = "Normal";
    private string _pipelineState = PipelineStateKind.Ready.ToString();
    private int _foupACount = 5;
    private int _foupBCount;
    private int _completedCount;
    private string _activeStationKey = "Home";
    private int _activeSlotLevel;
    private string _currentTransferDescription = "Ready";
    private string _activeWaferId = string.Empty;
    private string _waferIdOnBlade = string.Empty;
    private string _timingProfileName = SimulatorTimingProfile.Normal.Name;
    private bool _isSequencePaused;
    private bool _stepOnceRequested;
    private int _manualStepIndex;

    public MachineTwinViewModel(RuntimeCoordinator runtime)
    {
        _runtime = runtime;
        _physicalModel = DigitalTwinPhysicalModel.CreateDefault(runtime.Profile);
        _sequenceSteps = MachineTwinSequencePlan.CreateDefault(_physicalModel);
        _resetStep = MachineTwinSequencePlan.CreateResetStep(_physicalModel);
        EventLogLines = new ObservableCollection<string>();
        SequenceSpeedOptions = new ObservableCollection<string>(["Normal", "Realistic", "Fast", "Step"]);
        FoupASlots = new ObservableCollection<FoupSlotChipViewModel>(CreateSlots(_sequenceSteps[0].FoupASlots));
        FoupBSlots = new ObservableCollection<FoupSlotChipViewModel>(CreateSlots(_sequenceSteps[0].FoupBSlots));
        ChamberA = ChamberPipelineViewModel.From(_sequenceSteps[0].ChamberA);
        ChamberB = ChamberPipelineViewModel.From(_sequenceSteps[0].ChamberB);
        ChamberC = ChamberPipelineViewModel.From(_sequenceSteps[0].ChamberC);
        Stations = new ObservableCollection<MachineTwinStationViewModel>(
            _physicalModel.ThetaSwing.Stations.OrderBy(station => station.Order).Select(MachineTwinStationViewModel.From));
        RunTransferSequenceCommand = new AsyncRelayCommand(_ => RunTransferSequenceCommandAsync());
        PauseCommand = new RelayCommand(_ => PauseSequenceRun(), _ => IsSequenceRunning && !IsSequencePaused);
        ResumeCommand = new RelayCommand(_ => ResumeSequenceRun(), _ => IsSequenceRunning && IsSequencePaused);
        StepOnceCommand = new AsyncRelayCommand(_ => StepOnceAsync());
        StopCommand = new AsyncRelayCommand(_ => StopSequenceAsync());
        ResetCommand = new AsyncRelayCommand(_ => ResetSafeStateAsync());
        AutoStartCommand = new AsyncRelayCommand(_ => ViewModelErrorHandler.RunAsync(_runtime, nameof(MachineTwinViewModel), _runtime.StartAutoAsync));
        AutoStopCommand = new AsyncRelayCommand(_ => ViewModelErrorHandler.RunAsync(_runtime, nameof(MachineTwinViewModel), _runtime.StopAutoAsync));
        EmergencyStopCommand = new AsyncRelayCommand(_ => ViewModelErrorHandler.RunAsync(_runtime, nameof(MachineTwinViewModel), _runtime.EmergencyStopAsync));
        ApplySequenceStep(_sequenceSteps[0]);
    }

    public ObservableCollection<MachineTwinStationViewModel> Stations { get; }
    public ObservableCollection<FoupSlotChipViewModel> FoupASlots { get; }
    public ObservableCollection<FoupSlotChipViewModel> FoupBSlots { get; }
    public ObservableCollection<string> SequenceSpeedOptions { get; }
    public ObservableCollection<string> EventLogLines { get; }
    public ChamberPipelineViewModel ChamberA { get; }
    public ChamberPipelineViewModel ChamberB { get; }
    public ChamberPipelineViewModel ChamberC { get; }
    public string ScenarioName => _physicalModel.ScenarioName;
    public string EquipmentKind => _physicalModel.EquipmentKind;
    public string FeedbackBoundary => IsRealHardwareMode
        ? "Commanded / last-known state. Physical feedback depends on the real adapter."
        : "Simulator state is the source of truth.";
    public bool IsSequenceRunning
    {
        get => _isSequenceRunning;
        private set
        {
            if (SetProperty(ref _isSequenceRunning, value))
            {
                PauseCommand.RaiseCanExecuteChanged();
                ResumeCommand.RaiseCanExecuteChanged();
                StepOnceCommand.RaiseCanExecuteChanged();
                RunTransferSequenceCommand.RaiseCanExecuteChanged();
            }
        }
    }
    public bool IsSimulatorMode { get => _isSimulatorMode; private set => SetProperty(ref _isSimulatorMode, value); }
    public bool IsRealHardwareMode { get => _isRealHardwareMode; private set => SetProperty(ref _isRealHardwareMode, value); }
    public bool IsConnected { get => _isConnected; private set => SetProperty(ref _isConnected, value); }
    public string MachineState { get => _machineState; private set => SetProperty(ref _machineState, value); }
    public string CurrentStation { get => _currentStation; private set => SetProperty(ref _currentStation, value); }
    public string PreviousStation { get => _previousStation; private set => SetProperty(ref _previousStation, value); }
    public string NextStation { get => _nextStation; private set => SetProperty(ref _nextStation, value); }
    public string CurrentStepName { get => _currentStepName; private set => SetProperty(ref _currentStepName, value); }
    public string CurrentAction { get => _currentAction; private set => SetProperty(ref _currentAction, value); }
    public string OperationWafer { get => _operationWafer; private set => SetProperty(ref _operationWafer, value); }
    public string OperationSource { get => _operationSource; private set => SetProperty(ref _operationSource, value); }
    public string OperationDestination { get => _operationDestination; private set => SetProperty(ref _operationDestination, value); }
    public string OperationCurrentStep { get => _operationCurrentStep; private set => SetProperty(ref _operationCurrentStep, value); }
    public string OperationNextStep { get => _operationNextStep; private set => SetProperty(ref _operationNextStep, value); }
    public string RobotSequenceState { get => _robotSequenceState; private set => SetProperty(ref _robotSequenceState, value); }
    public string BladeSequenceState { get => _bladeSequenceState; private set => SetProperty(ref _bladeSequenceState, value); }
    public string VacuumDisplayState { get => _vacuumDisplayState; private set => SetProperty(ref _vacuumDisplayState, value); }
    public string ChamberADoorState { get => _chamberADoorState; private set => SetProperty(ref _chamberADoorState, value); }
    public string ChamberBDoorState { get => _chamberBDoorState; private set => SetProperty(ref _chamberBDoorState, value); }
    public string ChamberCDoorState { get => _chamberCDoorState; private set => SetProperty(ref _chamberCDoorState, value); }
    public double VisualThetaAngle { get => _visualThetaAngle; private set => SetProperty(ref _visualThetaAngle, value); }
    public string ThetaTargetName { get => _thetaTargetName; private set => SetProperty(ref _thetaTargetName, value); }
    public long PreservedThetaEncoderValue { get => _preservedThetaEncoderValue; private set => SetProperty(ref _preservedThetaEncoderValue, value); }
    public string ZState { get => _zState; private set => SetProperty(ref _zState, value); }
    public bool IsBladeExtended { get => _isBladeExtended; private set => SetProperty(ref _isBladeExtended, value); }
    public bool IsCylinderForward { get => _isCylinderForward; private set => SetProperty(ref _isCylinderForward, value); }
    public bool IsCylinderBackward { get => _isCylinderBackward; private set => SetProperty(ref _isCylinderBackward, value); }
    public bool IsVacuumOn { get => _isVacuumOn; private set => SetProperty(ref _isVacuumOn, value); }
    public bool IsWaferOnBlade { get => _isWaferOnBlade; private set => SetProperty(ref _isWaferOnBlade, value); }
    public bool IsWaferInFoupA1 { get => _isWaferInFoupA1; private set => SetProperty(ref _isWaferInFoupA1, value); }
    public bool IsWaferInChamberA { get => _isWaferInChamberA; private set => SetProperty(ref _isWaferInChamberA, value); }
    public bool IsWaferInChamberB { get => _isWaferInChamberB; private set => SetProperty(ref _isWaferInChamberB, value); }
    public bool IsWaferInChamberC { get => _isWaferInChamberC; private set => SetProperty(ref _isWaferInChamberC, value); }
    public bool IsWaferInFoupB1 { get => _isWaferInFoupB1; private set => SetProperty(ref _isWaferInFoupB1, value); }
    public bool ChamberADoorOpen { get => _chamberADoorOpen; private set => SetProperty(ref _chamberADoorOpen, value); }
    public bool ChamberBDoorOpen { get => _chamberBDoorOpen; private set => SetProperty(ref _chamberBDoorOpen, value); }
    public bool ChamberCDoorOpen { get => _chamberCDoorOpen; private set => SetProperty(ref _chamberCDoorOpen, value); }
    public bool TowerRed { get => _towerRed; private set => SetProperty(ref _towerRed, value); }
    public bool TowerYellow { get => _towerYellow; private set => SetProperty(ref _towerYellow, value); }
    public bool TowerGreen { get => _towerGreen; private set => SetProperty(ref _towerGreen, value); }
    public string AlarmSummary { get => _alarmSummary; private set => SetProperty(ref _alarmSummary, value); }
    public string SelectedSequenceSpeed { get => _selectedSequenceSpeed; set => SetProperty(ref _selectedSequenceSpeed, value); }
    public string PipelineState { get => _pipelineState; private set => SetProperty(ref _pipelineState, value); }
    public int FoupACount { get => _foupACount; private set => SetProperty(ref _foupACount, value); }
    public int FoupBCount { get => _foupBCount; private set => SetProperty(ref _foupBCount, value); }
    public int CompletedCount { get => _completedCount; private set => SetProperty(ref _completedCount, value); }
    public string ActiveStationKey { get => _activeStationKey; private set => SetProperty(ref _activeStationKey, value); }
    public int ActiveSlotLevel { get => _activeSlotLevel; private set => SetProperty(ref _activeSlotLevel, value); }
    public string CurrentTransferDescription { get => _currentTransferDescription; private set => SetProperty(ref _currentTransferDescription, value); }
    public string ActiveWaferId { get => _activeWaferId; private set => SetProperty(ref _activeWaferId, value); }
    public string WaferIdOnBlade { get => _waferIdOnBlade; private set => SetProperty(ref _waferIdOnBlade, value); }
    public string TimingProfileName { get => _timingProfileName; private set => SetProperty(ref _timingProfileName, value); }
    public bool IsSequencePaused
    {
        get => _isSequencePaused;
        private set
        {
            if (SetProperty(ref _isSequencePaused, value))
            {
                PauseCommand.RaiseCanExecuteChanged();
                ResumeCommand.RaiseCanExecuteChanged();
                UpdateTowerAndAlarmForPlayback();
            }
        }
    }
    public string ModeLabel => IsSimulatorMode ? "SIMULATOR" : "REAL HARDWARE";
    public string ConnectionKindLabel => IsSimulatorMode ? "Sim Link" : "EtherCAT";
    public string ConnectionLabel => IsSimulatorMode
        ? IsConnected ? "Sim Ready" : "Sim Idle"
        : IsConnected ? "Connected" : "Disconnected";
    public double BladeLength => IsBladeExtended ? 245 : 92;
    public double BladeScaleY => IsBladeExtended ? 1.0 : 0.38;
    public string CylinderState => $"{BladeSequenceState} / {(IsCylinderForward ? "Cylinder forward" : "Cylinder backward")}";
    public string VacuumState => VacuumDisplayState switch
    {
        nameof(VacuumSequenceState.SuctionOn) => "Suction ON / wafer pickup enabled",
        nameof(VacuumSequenceState.ExhaustOrRelease) => "Exhaust / release active",
        _ => "Vacuum OFF"
    };
    public string WaferSummary => IsWaferOnBlade
        ? $"{WaferIdOnBlade} on blade"
        : CompletedCount == 5 ? "All 5 wafers in FOUP B"
        : $"{FoupACount}/5 in FOUP A, {FoupBCount}/5 in FOUP B";
    public string FoupASummary => $"FOUP A: {FoupACount}/5";
    public string FoupBSummary => $"FOUP B: {FoupBCount}/5";
    public string FoupASlotMask => BuildSlotMask(FoupASlots);
    public string FoupBSlotMask => BuildSlotMask(FoupBSlots);

    public AsyncRelayCommand RunTransferSequenceCommand { get; }
    public RelayCommand PauseCommand { get; }
    public RelayCommand ResumeCommand { get; }
    public AsyncRelayCommand StepOnceCommand { get; }
    public AsyncRelayCommand StopCommand { get; }
    public AsyncRelayCommand ResetCommand { get; }
    public AsyncRelayCommand AutoStartCommand { get; }
    public AsyncRelayCommand AutoStopCommand { get; }
    public AsyncRelayCommand EmergencyStopCommand { get; }

    public void Refresh(EquipmentStatus status)
    {
        IsSimulatorMode = status.Mode == OperatingMode.Simulator;
        IsRealHardwareMode = status.Mode == OperatingMode.RealHardware;
        IsConnected = status.IsConnected;
        MachineState = status.MachineState.ToString();
        RaiseConnectionLabels();
        OnPropertyChanged(nameof(FeedbackBoundary));

        if (IsSequenceRunning || PipelineState == PipelineStateKind.Completed.ToString())
        {
            // 재생 중/완료 상태의 경광봉은 파이프라인 상태가 기준입니다. 주기 상태 갱신이 완료 알람을 덮지 않게 막습니다.
            UpdateTowerAndAlarmForPlayback();
            return;
        }

        AlarmSummary = status.AlarmSummary;
        TowerRed = status.Outputs.TryGetValue(IoPoint.TowerRed, out var red) && red;
        TowerYellow = status.Outputs.TryGetValue(IoPoint.TowerYellow, out var yellow) && yellow;
        TowerGreen = status.Outputs.TryGetValue(IoPoint.TowerGreen, out var green) && green;
        IsCylinderForward = status.Outputs.TryGetValue(IoPoint.CylinderForward, out var forward) && forward;
        IsCylinderBackward = !IsCylinderForward && status.Outputs.TryGetValue(IoPoint.CylinderBackward, out var backward) && backward;
        IsBladeExtended = IsCylinderForward;
        IsVacuumOn = status.Outputs.TryGetValue(IoPoint.VacuumSuction, out var vacuum) && vacuum;
        ChamberADoorOpen = status.Inputs.TryGetValue(IoPoint.ChamberADoorOpenSensor, out var doorA) && doorA;
        ChamberBDoorOpen = status.Inputs.TryGetValue(IoPoint.ChamberBDoorOpenSensor, out var doorB) && doorB;
        ChamberCDoorOpen = status.Inputs.TryGetValue(IoPoint.ChamberCDoorOpenSensor, out var doorC) && doorC;
        UpdateComputedProperties();
    }

    public async Task RunTransferSequenceForCaptureAsync(Func<MachineTwinSequenceStep, Task> captureStep, CancellationToken cancellationToken = default)
    {
        await EnsureSimulatorReadyAsync(cancellationToken).ConfigureAwait(true);
        foreach (var step in _sequenceSteps)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await ApplySimulatorStepAsync(step, cancellationToken).ConfigureAwait(true);
            await captureStep(step).ConfigureAwait(true);
        }

        // Reset is not part of normal runtime playback. Capture modes include it explicitly
        // so evidence can still show the manual Reset target state without changing runtime UX.
        cancellationToken.ThrowIfCancellationRequested();
        await ApplySimulatorStepAsync(_resetStep, cancellationToken).ConfigureAwait(true);
        await captureStep(_resetStep).ConfigureAwait(true);
    }

    public MachineTwinStateTraceEntry CreateTraceEntry(MachineTwinSequenceStep step, string screenshotPath) => new(
        step.StepIndex,
        step.StepName,
        DateTimeOffset.Now,
        IsSimulatorMode,
        IsRealHardwareMode,
        IsConnected,
            MachineState,
        CurrentStation,
        PreviousStation,
        NextStation,
        CurrentStepName,
        CurrentAction,
        RobotSequenceState,
        BladeSequenceState,
        VacuumDisplayState,
        ChamberADoorState,
        ChamberBDoorState,
        ChamberCDoorState,
        ThetaTargetName,
        VisualThetaAngle,
        PreservedThetaEncoderValue,
        ZState,
        IsBladeExtended,
        IsCylinderForward,
        IsCylinderBackward,
        IsVacuumOn,
        IsWaferOnBlade,
        IsWaferInFoupA1,
        IsWaferInChamberA,
        IsWaferInChamberB,
        IsWaferInChamberC,
        IsWaferInFoupB1,
        ChamberADoorOpen,
        ChamberBDoorOpen,
        ChamberCDoorOpen,
        TowerRed,
        TowerYellow,
            TowerGreen,
            AlarmSummary,
            step.EventLogMessage,
            step.PipelineState,
            step.FoupACount,
            step.FoupBCount,
            step.CompletedCount,
            step.TotalWafers,
            step.CurrentTransferDescription,
            step.ActiveWaferId,
            step.WaferIdOnBlade,
            step.VacuumState,
            step.WaferIds,
            step.TimingProfileName,
            FormatSlots(step.FoupASlots),
            FormatSlots(step.FoupBSlots),
            FormatChamber(step.ChamberA),
            FormatChamber(step.ChamberB),
            FormatChamber(step.ChamberC),
            screenshotPath);

    private async Task RunTransferSequenceCommandAsync()
    {
        _sequenceCts?.Cancel();
        _sequenceCts = new CancellationTokenSource();
        try
        {
            IsSequenceRunning = true;
            IsSequencePaused = false;
            _manualStepIndex = 0;
            await EnsureSimulatorReadyAsync(_sequenceCts.Token).ConfigureAwait(true);
            for (var i = 0; i < _sequenceSteps.Count; i++)
            {
                var stepOnce = await WaitForSequenceGateAsync(_sequenceCts.Token).ConfigureAwait(true);
                _sequenceCts.Token.ThrowIfCancellationRequested();
                var step = _sequenceSteps[i];
                await ApplySimulatorStepAsync(step, _sequenceCts.Token).ConfigureAwait(true);
                _manualStepIndex = Math.Min(i + 1, _sequenceSteps.Count - 1);
                if (stepOnce)
                {
                    IsSequencePaused = true;
                    continue;
                }

                await DelayWithSequenceGateAsync(GetRuntimeDelayForSelectedSpeed(step), _sequenceCts.Token).ConfigureAwait(true);
            }
        }
        catch (OperationCanceledException)
        {
            AddEvent("Sequence run canceled.");
        }
        finally
        {
            IsSequenceRunning = false;
            IsSequencePaused = false;
        }
    }

    private void PauseSequenceRun()
    {
        IsSequencePaused = true;
        AddEvent("Sequence run paused.");
    }

    private void ResumeSequenceRun()
    {
        _stepOnceRequested = false;
        IsSequencePaused = false;
        AddEvent("Sequence run resumed.");
    }

    private async Task StepOnceAsync()
    {
        if (IsSequenceRunning)
        {
            _stepOnceRequested = true;
            IsSequencePaused = false;
            AddEvent("Step once requested.");
            return;
        }

        await EnsureSimulatorReadyAsync(CancellationToken.None).ConfigureAwait(true);
        var index = Math.Clamp(_manualStepIndex, 0, _sequenceSteps.Count - 1);
        var step = _sequenceSteps[index];
        await ApplySimulatorStepAsync(step, CancellationToken.None).ConfigureAwait(true);
        _manualStepIndex = Math.Min(index + 1, _sequenceSteps.Count - 1);
        await Task.Delay(GetManualStepVisualDelay(step), CancellationToken.None).ConfigureAwait(true);
    }

    private async Task<bool> WaitForSequenceGateAsync(CancellationToken cancellationToken)
    {
        while (IsSequencePaused && !_stepOnceRequested)
        {
            await Task.Delay(100, cancellationToken).ConfigureAwait(true);
        }

        if (!_stepOnceRequested)
        {
            return false;
        }

        _stepOnceRequested = false;
        return true;
    }

    private async Task DelayWithSequenceGateAsync(int totalDelayMs, CancellationToken cancellationToken)
    {
        var remaining = Math.Max(0, totalDelayMs);
        while (remaining > 0)
        {
            if (await WaitForSequenceGateAsync(cancellationToken).ConfigureAwait(true))
            {
                IsSequencePaused = true;
                return;
            }

            var slice = Math.Min(100, remaining);
            await Task.Delay(slice, cancellationToken).ConfigureAwait(true);
            remaining -= slice;
        }
    }

    private Task StopSequenceAsync()
    {
        _sequenceCts?.Cancel();
        IsSequenceRunning = false;
        IsSequencePaused = false;
        AddEvent("Stop requested. Transfer sequence canceled.");
        return ViewModelErrorHandler.RunAsync(_runtime, nameof(MachineTwinViewModel), _runtime.StopAutoAsync);
    }

    private async Task ResetSafeStateAsync()
    {
        _sequenceCts?.Cancel();
        IsSequenceRunning = false;
        IsSequencePaused = false;
        _manualStepIndex = 0;
        await ApplySimulatorStepAsync(_resetStep, CancellationToken.None).ConfigureAwait(true);
        await ViewModelErrorHandler.RunAsync(_runtime, nameof(MachineTwinViewModel), _runtime.ResetAsync).ConfigureAwait(true);
    }

    private async Task EnsureSimulatorReadyAsync(CancellationToken cancellationToken)
    {
        if (_runtime.Controller.Mode == OperatingMode.RealHardware && _runtime.Controller.IsConnected)
        {
            throw new InvalidOperationException("Transfer sequence run is blocked while Real Hardware mode is connected.");
        }

        if (_runtime.Controller.Mode != OperatingMode.Simulator)
        {
            _runtime.Controller.SetMode(OperatingMode.Simulator);
        }

        if (!_runtime.Controller.IsConnected)
        {
            await _runtime.ConnectAsync(cancellationToken).ConfigureAwait(true);
        }

        await _runtime.Controller.ServoOnAsync(cancellationToken).ConfigureAwait(true);
        await _runtime.Controller.HomeAxisAsync(AxisId.Z, cancellationToken).ConfigureAwait(true);
        await _runtime.Controller.HomeAxisAsync(AxisId.Theta, cancellationToken).ConfigureAwait(true);
        _runtime.Safety.MarkHomed(AxisId.Z);
        _runtime.Safety.MarkHomed(AxisId.Theta);
        IsConnected = true;
        IsSimulatorMode = true;
        IsRealHardwareMode = false;
        MachineState = SemiTool.Domain.MachineState.Manual.ToString();
        RaiseConnectionLabels();
    }

    private async Task ApplySimulatorStepAsync(MachineTwinSequenceStep step, CancellationToken cancellationToken)
    {
        ApplySequenceStep(step);
        await ApplySimulatorOutputsAsync(step, cancellationToken).ConfigureAwait(true);
        AddEvent(step.EventLogMessage);
    }

    private void ApplySequenceStep(MachineTwinSequenceStep step)
    {
        CurrentStation = step.CurrentStation;
        PreviousStation = step.PreviousStation;
        NextStation = step.NextStation;
        CurrentStepName = step.CurrentStepName;
        CurrentAction = step.CurrentAction;
        ApplyOperationStrip(step);
        RobotSequenceState = step.RobotState;
        BladeSequenceState = step.BladeState;
        VacuumDisplayState = step.VacuumSequenceState;
        ChamberADoorState = step.ChamberADoorState;
        ChamberBDoorState = step.ChamberBDoorState;
        ChamberCDoorState = step.ChamberCDoorState;
        ThetaTargetName = step.CurrentStation;
        // 런타임 화면은 항상 DigitalTwinPhysicalModel의 HMI 전용 각도를 사용한다.
        // 실제 엔코더 티칭값은 PreservedThetaEncoderValue에 그대로 남기고 UI 각도로 해석하지 않는다.
        VisualThetaAngle = ResolveRuntimeVisualThetaAngle(step);
        PreservedThetaEncoderValue = step.PreservedThetaEncoderValue;
        ZState = step.ZState;
        IsBladeExtended = step.IsBladeExtended;
        IsCylinderForward = step.IsCylinderForward;
        IsCylinderBackward = step.IsCylinderBackward;
        IsVacuumOn = step.IsVacuumOn;
        IsWaferOnBlade = step.IsWaferOnBlade;
        IsWaferInFoupA1 = step.IsWaferInFoupA1;
        IsWaferInChamberA = step.IsWaferInChamberA;
        IsWaferInChamberB = step.IsWaferInChamberB;
        IsWaferInChamberC = step.IsWaferInChamberC;
        IsWaferInFoupB1 = step.IsWaferInFoupB1;
        ChamberADoorOpen = step.ChamberADoorOpen;
        ChamberBDoorOpen = step.ChamberBDoorOpen;
        ChamberCDoorOpen = step.ChamberCDoorOpen;
        PipelineState = step.PipelineState;
        FoupACount = step.FoupACount;
        FoupBCount = step.FoupBCount;
        CompletedCount = step.CompletedCount;
        ActiveStationKey = step.StationKey;
        ActiveSlotLevel = ResolveActiveSlotLevel(step);
        CurrentTransferDescription = step.CurrentTransferDescription;
        ActiveWaferId = step.ActiveWaferId;
        WaferIdOnBlade = step.WaferIdOnBlade;
        TimingProfileName = step.TimingProfileName;
        UpdateSlots(FoupASlots, step.FoupASlots);
        UpdateSlots(FoupBSlots, step.FoupBSlots);
        ChamberA.Update(step.ChamberA);
        ChamberB.Update(step.ChamberB);
        ChamberC.Update(step.ChamberC);
        SelectStation(step.CurrentStation);
        UpdateTowerAndAlarmForPlayback();
        UpdateComputedProperties();
    }

    private async Task ApplySimulatorOutputsAsync(MachineTwinSequenceStep step, CancellationToken cancellationToken)
    {
        if (!_runtime.Controller.IsConnected || _runtime.Controller.Mode != OperatingMode.Simulator)
        {
            return;
        }

        // Keep the running Digital Twin and I/O Monitor aligned through named IoPoint writes, never raw DO numbers.
        await _runtime.Controller.WriteDigitalOutputAsync(IoPoint.CylinderForward, step.IsCylinderForward, cancellationToken).ConfigureAwait(true);
        await _runtime.Controller.WriteDigitalOutputAsync(IoPoint.CylinderBackward, step.IsCylinderBackward, cancellationToken).ConfigureAwait(true);
        await _runtime.Controller.WriteDigitalOutputAsync(IoPoint.VacuumSuction, step.IsVacuumSuctionOutputOn, cancellationToken).ConfigureAwait(true);
        await _runtime.Controller.WriteDigitalOutputAsync(IoPoint.VacuumExhaust, step.IsVacuumExhaustOutputOn, cancellationToken).ConfigureAwait(true);
        var (towerRed, towerYellow, towerGreen) = ResolveTowerState();
        await _runtime.Controller.WriteDigitalOutputAsync(IoPoint.TowerRed, towerRed, cancellationToken).ConfigureAwait(true);
        await _runtime.Controller.WriteDigitalOutputAsync(IoPoint.TowerYellow, towerYellow, cancellationToken).ConfigureAwait(true);
        await _runtime.Controller.WriteDigitalOutputAsync(IoPoint.TowerGreen, towerGreen, cancellationToken).ConfigureAwait(true);
        await _runtime.Controller.SetSimulatorInputAsync(IoPoint.ChamberADoorOpenSensor, step.ChamberADoorOpen, cancellationToken).ConfigureAwait(true);
        await _runtime.Controller.SetSimulatorInputAsync(IoPoint.ChamberBDoorOpenSensor, step.ChamberBDoorOpen, cancellationToken).ConfigureAwait(true);
        await _runtime.Controller.SetSimulatorInputAsync(IoPoint.ChamberCDoorOpenSensor, step.ChamberCDoorOpen, cancellationToken).ConfigureAwait(true);
        await _runtime.Controller.MoveAxisAbsoluteAsync(AxisId.Theta, step.PreservedThetaEncoderValue, cancellationToken).ConfigureAwait(true);
        var pose = _runtime.Profile.GetPose(step.StationKey);
        var z = step.IsZWorkPosition ? pose.ZWork : pose.ZSafe;
        await _runtime.Controller.MoveAxisAbsoluteAsync(AxisId.Z, z, cancellationToken).ConfigureAwait(true);
    }

    private double ResolveRuntimeVisualThetaAngle(MachineTwinSequenceStep step)
    {
        var station = _physicalModel.ThetaSwing.Stations
            .FirstOrDefault(candidate => string.Equals(candidate.PoseKey, step.StationKey, StringComparison.OrdinalIgnoreCase));

        return station?.VisualArcPositionDegrees ?? step.VisualThetaAngle;
    }

    private void UpdateTowerAndAlarmForPlayback()
    {
        var (red, yellow, green) = ResolveTowerState();
        TowerRed = red;
        TowerYellow = yellow;
        TowerGreen = green;

        AlarmSummary = red
            ? "Sequence paused - operator attention required"
            : yellow ? "Cycle complete alarm: FOUP B 5/5"
            : "No active alarms";
    }

    private (bool Red, bool Yellow, bool Green) ResolveTowerState()
    {
        if (IsSequencePaused)
        {
            return (true, false, false);
        }

        if (PipelineState == PipelineStateKind.Completed.ToString())
        {
            return (false, true, false);
        }

        if (PipelineState == PipelineStateKind.Running.ToString())
        {
            return (false, false, true);
        }

        return (false, false, false);
    }

    private void AddEvent(string message)
    {
        var line = $"{DateTimeOffset.Now:HH:mm:ss}  {message}";
        EventLogLines.Insert(0, line);
        while (EventLogLines.Count > 8)
        {
            EventLogLines.RemoveAt(EventLogLines.Count - 1);
        }
    }

    private void ApplyOperationStrip(MachineTwinSequenceStep step)
    {
        var (source, destination) = ParseTransfer(step.CurrentTransferDescription, step.CurrentStation, step.NextStation);
        OperationWafer = string.IsNullOrWhiteSpace(step.ActiveWaferId) ? "-" : step.ActiveWaferId;
        OperationSource = source;
        OperationDestination = destination;
        OperationCurrentStep = step.PipelineState == PipelineStateKind.Completed.ToString()
            ? "Sequence Complete"
            : NormalizeOperationStep(step);
        OperationNextStep = BuildNextStepPreview(step);
    }

    private static (string Source, string Destination) ParseTransfer(string description, string currentStation, string nextStation)
    {
        if (!string.IsNullOrWhiteSpace(description) && description.Contains("->", StringComparison.Ordinal))
        {
            var parts = description.Split("->", 2, StringSplitOptions.TrimEntries);
            return (CleanOperationField(parts[0], currentStation), CleanOperationField(parts[1], nextStation));
        }

        return (CleanOperationField(currentStation, "-"), CleanOperationField(nextStation, "-"));
    }

    private static string CleanOperationField(string value, string fallback)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Equals("Ready", StringComparison.OrdinalIgnoreCase))
        {
            return fallback;
        }

        return value;
    }

    private static string NormalizeOperationStep(MachineTwinSequenceStep step)
    {
        if (step.RobotState == nameof(SemiTool.Domain.RobotSequenceState.MovingTheta))
        {
            return $"Theta moving to {step.CurrentStation}";
        }

        if (step.ZState.StartsWith("Z Work", StringComparison.OrdinalIgnoreCase))
        {
            return "Z at Work position";
        }

        if (step.BladeState is nameof(SemiTool.Domain.BladeSequenceState.Extending) or nameof(SemiTool.Domain.BladeSequenceState.Extended))
        {
            return step.IsWaferOnBlade ? "Blade extended with wafer" : "Blade extended";
        }

        if (step.BladeState == nameof(SemiTool.Domain.BladeSequenceState.Retracting))
        {
            return "Blade retracting";
        }

        if (step.VacuumSequenceState == nameof(VacuumSequenceState.SuctionOn))
        {
            return "Vacuum suction";
        }

        if (step.VacuumSequenceState == nameof(VacuumSequenceState.ExhaustOrRelease))
        {
            return "Vacuum release";
        }

        return string.IsNullOrWhiteSpace(step.CurrentStepName) ? step.PipelineState : step.CurrentStepName;
    }

    private static string BuildNextStepPreview(MachineTwinSequenceStep step)
    {
        if (step.PipelineState == PipelineStateKind.Completed.ToString())
        {
            return "Hold completed state until Reset";
        }

        if (step.CurrentAction.Contains("door opening", StringComparison.OrdinalIgnoreCase))
        {
            return "Confirm door open, then extend blade";
        }

        if (step.CurrentAction.Contains("Blade extending", StringComparison.OrdinalIgnoreCase))
        {
            return step.IsWaferOnBlade ? "Vacuum release / place wafer" : "Vacuum suction / pick wafer";
        }

        if (step.CurrentAction.Contains("placed", StringComparison.OrdinalIgnoreCase))
        {
            return "Retract blade before door close";
        }

        if (step.CurrentAction.Contains("processing", StringComparison.OrdinalIgnoreCase))
        {
            return "Wait for process complete";
        }

        return string.IsNullOrWhiteSpace(step.NextStation) || step.NextStation == "-"
            ? "Await scheduler decision"
            : $"Next station: {step.NextStation}";
    }

    private void UpdateComputedProperties()
    {
        OnPropertyChanged(nameof(BladeLength));
        OnPropertyChanged(nameof(BladeScaleY));
        OnPropertyChanged(nameof(CylinderState));
        OnPropertyChanged(nameof(VacuumState));
        OnPropertyChanged(nameof(WaferSummary));
        OnPropertyChanged(nameof(FoupASummary));
        OnPropertyChanged(nameof(FoupBSummary));
        OnPropertyChanged(nameof(FoupASlotMask));
        OnPropertyChanged(nameof(FoupBSlotMask));
        OnPropertyChanged(nameof(FeedbackBoundary));
    }

    private void RaiseConnectionLabels()
    {
        OnPropertyChanged(nameof(ModeLabel));
        OnPropertyChanged(nameof(ConnectionKindLabel));
        OnPropertyChanged(nameof(ConnectionLabel));
    }

    private void SelectStation(string displayName)
    {
        foreach (var station in Stations)
        {
            station.IsCurrent = string.Equals(station.DisplayName, displayName, StringComparison.OrdinalIgnoreCase);
        }
    }

    private int GetRuntimeDelayForSelectedSpeed(MachineTwinSequenceStep step) =>
        SelectedSequenceSpeed switch
        {
            _ when string.Equals(step.StepName, "Reset Safe State", StringComparison.OrdinalIgnoreCase) => 800,
            "Fast" => Math.Max(180, Math.Min(step.DelayMs / 6, 450)),
            "Step" => 1200,
            _ => Math.Max(800, step.DelayMs)
        };

    private int GetManualStepVisualDelay(MachineTwinSequenceStep step) =>
        // Step Once에서도 회전 -> Z -> 블레이드 전진 순서가 눈에 보이도록 최소 표시 시간을 보장합니다.
        Math.Clamp(GetRuntimeDelayForSelectedSpeed(step), 650, 1100);

    private static IEnumerable<FoupSlotChipViewModel> CreateSlots(IEnumerable<WaferPipelineSlot> slots) =>
        slots.Select(FoupSlotChipViewModel.From);

    private static void UpdateSlots(IList<FoupSlotChipViewModel> target, IReadOnlyList<WaferPipelineSlot> source)
    {
        for (var i = 0; i < source.Count; i++)
        {
            target[i].Update(source[i]);
        }
    }

    private static int ResolveActiveSlotLevel(MachineTwinSequenceStep step)
    {
        if (!step.IsZWorkPosition ||
            !step.StationKey.StartsWith("Foup", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        return ParseSlotLevel(step.ZState);
    }

    private static int ParseSlotLevel(string text)
    {
        foreach (var prefix in new[] { "Slot A", "Slot B" })
        {
            var start = text.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
            var digitIndex = start + prefix.Length;
            if (start >= 0 &&
                digitIndex < text.Length &&
                char.IsDigit(text[digitIndex]) &&
                text[digitIndex] is >= '1' and <= '5')
            {
                return text[digitIndex] - '0';
            }
        }

        return 0;
    }

    private static string BuildSlotMask(IEnumerable<FoupSlotChipViewModel> slots) =>
        string.Concat(slots.Select(slot => slot.HasWafer ? '1' : '0'));

    private static string FormatSlots(IEnumerable<WaferPipelineSlot> slots) =>
        string.Join("; ", slots.Select(slot => $"{slot.SlotName}:{(slot.HasWafer ? slot.WaferId : "Empty")}:{slot.State}"));

    private static string FormatChamber(ChamberPipelineSnapshot chamber) =>
        $"{chamber.ChamberName}:{chamber.ProcessState}:{chamber.WaferId}:{chamber.RecipeName}:{chamber.CurrentStep}:{chamber.RemainingSeconds}s:{chamber.ProgressPercent:F0}%";

}

public sealed class MachineTwinStationViewModel : ObservableObject
{
    private bool _isCurrent;

    private MachineTwinStationViewModel(string displayName, string role, long thetaEncoderPosition, double visualArcPositionDegrees)
    {
        DisplayName = displayName;
        Role = role;
        ThetaEncoderPosition = thetaEncoderPosition;
        VisualArcPositionDegrees = visualArcPositionDegrees;
    }

    public string DisplayName { get; }
    public string Role { get; }
    public long ThetaEncoderPosition { get; }
    public double VisualArcPositionDegrees { get; }
    public bool IsCurrent { get => _isCurrent; set => SetProperty(ref _isCurrent, value); }

    public static MachineTwinStationViewModel From(RobotSwingStation station) =>
        new(station.DisplayName, station.Role, station.ThetaEncoderPosition, station.VisualArcPositionDegrees);
}

public sealed class FoupSlotChipViewModel : ObservableObject
{
    private bool _hasWafer;
    private string _waferId = string.Empty;
    private string _state = "Empty";
    private bool _isActive;

    private FoupSlotChipViewModel(string label, bool hasWafer, string waferId, string state, bool isActive)
    {
        Label = label;
        _hasWafer = hasWafer;
        _waferId = waferId;
        _state = state;
        _isActive = isActive;
    }

    public string Label { get; }
    public bool HasWafer
    {
        get => _hasWafer;
        set
        {
            if (SetProperty(ref _hasWafer, value))
            {
                OnPropertyChanged(nameof(SlotDisplay));
            }
        }
    }

    public string WaferId
    {
        get => _waferId;
        private set
        {
            if (SetProperty(ref _waferId, value))
            {
                OnPropertyChanged(nameof(SlotDisplay));
            }
        }
    }

    public string State { get => _state; private set => SetProperty(ref _state, value); }
    public bool IsActive { get => _isActive; private set => SetProperty(ref _isActive, value); }
    public string SlotDisplay => HasWafer ? WaferId : "Empty";

    public static FoupSlotChipViewModel From(WaferPipelineSlot slot) =>
        new(slot.SlotName, slot.HasWafer, slot.WaferId, slot.State, slot.IsActive);

    public void Update(WaferPipelineSlot slot)
    {
        HasWafer = slot.HasWafer;
        WaferId = slot.WaferId;
        State = slot.State;
        IsActive = slot.IsActive;
    }
}

public sealed class ChamberPipelineViewModel : ObservableObject
{
    private bool _hasWafer;
    private string _waferId = string.Empty;
    private string _processState = "Empty";
    private string _recipeName = string.Empty;
    private string _currentStep = "-";
    private int _remainingTime;
    private double _progressPercent;
    private bool _doorOpen;

    private ChamberPipelineViewModel(string chamberName, string role)
    {
        ChamberName = chamberName;
        Role = role;
    }

    public string ChamberName { get; }
    public string Role { get; }
    public bool HasWafer { get => _hasWafer; private set => SetProperty(ref _hasWafer, value); }
    public string WaferId { get => _waferId; private set => SetProperty(ref _waferId, value); }
    public string ProcessState { get => _processState; private set => SetProperty(ref _processState, value); }
    public string RecipeName { get => _recipeName; private set => SetProperty(ref _recipeName, value); }
    public string CurrentStep { get => _currentStep; private set => SetProperty(ref _currentStep, value); }
    public int RemainingTime { get => _remainingTime; private set => SetProperty(ref _remainingTime, value); }
    public double ProgressPercent { get => _progressPercent; private set => SetProperty(ref _progressPercent, value); }
    public bool DoorOpen { get => _doorOpen; private set => SetProperty(ref _doorOpen, value); }
    public string Summary => HasWafer ? $"{WaferId} / {ProcessState}" : "Empty";

    public static ChamberPipelineViewModel From(ChamberPipelineSnapshot source)
    {
        var viewModel = new ChamberPipelineViewModel(source.ChamberName, source.Role);
        viewModel.Update(source);
        return viewModel;
    }

    public void Update(ChamberPipelineSnapshot source)
    {
        HasWafer = source.HasWafer;
        WaferId = source.WaferId;
        ProcessState = source.ProcessState;
        RecipeName = source.RecipeName;
        CurrentStep = source.CurrentStep;
        RemainingTime = source.RemainingSeconds;
        ProgressPercent = source.ProgressPercent;
        DoorOpen = source.DoorOpen;
        OnPropertyChanged(nameof(Summary));
    }
}

public sealed record MachineTwinStateTraceEntry(
    int StepIndex,
    string StepName,
    DateTimeOffset Timestamp,
    bool IsSimulatorMode,
    bool IsRealHardwareMode,
    bool IsConnected,
    string MachineState,
    string CurrentStation,
    string PreviousStation,
    string NextStation,
    string CurrentStepName,
    string CurrentAction,
    string RobotState,
    string BladeState,
    string VacuumDisplayState,
    string ChamberADoorState,
    string ChamberBDoorState,
    string ChamberCDoorState,
    string ThetaTargetName,
    double VisualThetaAngle,
    long PreservedThetaEncoderValue,
    string ZState,
    bool IsBladeExtended,
    bool IsCylinderForward,
    bool IsCylinderBackward,
    bool IsVacuumOn,
    bool IsWaferOnBlade,
    bool IsWaferInFoupA1,
    bool IsWaferInChamberA,
    bool IsWaferInChamberB,
    bool IsWaferInChamberC,
    bool IsWaferInFoupB1,
    bool ChamberADoorOpen,
    bool ChamberBDoorOpen,
    bool ChamberCDoorOpen,
    bool TowerRed,
    bool TowerYellow,
    bool TowerGreen,
    string AlarmSummary,
    string EventLogMessage,
    string PipelineState,
    int FoupACount,
    int FoupBCount,
    int CompletedCount,
    int TotalWafers,
    string CurrentTransferDescription,
    string ActiveWaferId,
    string WaferIdOnBlade,
    string VacuumState,
    string WaferIds,
    string TimingProfileName,
    string FoupASlotStates,
    string FoupBSlotStates,
    string ChamberAState,
    string ChamberBState,
    string ChamberCState,
    string ScreenshotPath);
