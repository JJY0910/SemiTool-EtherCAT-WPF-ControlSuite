using System.Collections.ObjectModel;
using System.Windows.Input;
using SemiTool.Hmi.Wpf.ViewModels;

namespace SemiTool.Hmi.Wpf.DesignTime;

/// <summary>
/// Visual Studio Designer sample state for MachineTwinView.
/// </summary>
/// <remarks>
/// This class intentionally mirrors the public binding surface of
/// MachineTwinViewModel without depending on RuntimeCoordinator. It is static
/// sample data only: it never connects to EtherCAT, never loads the vendor DLL,
/// and never changes simulator or real-hardware behavior.
/// </remarks>
public sealed class DesignMachineTwinViewModel
{
    public DesignMachineTwinViewModel()
    {
        FoupASlots = DesignMachineTwinData.CreateFoupASlots();
        FoupBSlots = DesignMachineTwinData.CreateFoupBSlots();
        Stations = DesignMachineTwinData.CreateStations();
        EventLogLines = DesignMachineTwinData.CreateEventLogLines();
        SequenceSpeedOptions = new ObservableCollection<string>(["Normal", "Realistic", "Fast", "Step"]);

        ChamberA = new DesignChamberPipelineViewModel(
            "Chamber A",
            "Pre-Clean",
            false,
            string.Empty,
            "Empty",
            "PreClean_Default",
            "Ready",
            0,
            0,
            false);

        ChamberB = new DesignChamberPipelineViewModel(
            "Chamber B",
            "CMP Main",
            false,
            string.Empty,
            "Empty",
            "CMP_Main",
            "Ready",
            0,
            0,
            false);

        ChamberC = new DesignChamberPipelineViewModel(
            "Chamber C",
            "Post-Clean & Dry",
            false,
            string.Empty,
            "Empty",
            "PostClean_Dry",
            "Ready",
            0,
            0,
            false);

        RunTransferSequenceCommand = CreateNoOpCommand();
        PauseCommand = CreateNoOpCommand();
        ResumeCommand = CreateNoOpCommand();
        StepOnceCommand = CreateNoOpCommand();
        StopCommand = CreateNoOpCommand();
        ResetCommand = CreateNoOpCommand();
        AutoStartCommand = CreateNoOpCommand();
        AutoStopCommand = CreateNoOpCommand();
        EmergencyStopCommand = CreateNoOpCommand();
    }

    public ObservableCollection<DesignMachineTwinStationViewModel> Stations { get; }
    public ObservableCollection<DesignFoupSlotChipViewModel> FoupASlots { get; }
    public ObservableCollection<DesignFoupSlotChipViewModel> FoupBSlots { get; }
    public ObservableCollection<string> SequenceSpeedOptions { get; }
    public ObservableCollection<string> EventLogLines { get; }
    public DesignChamberPipelineViewModel ChamberA { get; }
    public DesignChamberPipelineViewModel ChamberB { get; }
    public DesignChamberPipelineViewModel ChamberC { get; }

    public string ScenarioName => "Visual Studio Designer sample";
    public string EquipmentKind => "Wafer transfer robot sequence monitor";
    public string FeedbackBoundary => "Design-time sample only. Five unique wafers total; runtime sequence animates the real ViewModel.";
    public bool IsSequenceRunning => false;
    public bool IsSimulatorMode => true;
    public bool IsRealHardwareMode => false;
    public bool IsConnected => false;
    public string MachineState => "DesignerPreview";
    public string CurrentStation => "Home / Start";
    public string PreviousStation => "-";
    public string NextStation => "FOUP A";
    public string CurrentStepName => "Designer sample: startup simulator state";
    public string CurrentAction => "Pipeline ready: FOUP A 5 wafers, FOUP B empty";
    public string OperationWafer => "-";
    public string OperationSource => "Home / Start";
    public string OperationDestination => "FOUP A";
    public string OperationCurrentStep => "Ready";
    public string OperationNextStep => "Move theta to FOUP A";
    public string RobotSequenceState => "Idle";
    public string BladeSequenceState => "Retracted";
    public string VacuumDisplayState => "Off";
    public string ChamberADoorState => "Closed";
    public string ChamberBDoorState => "Closed";
    public string ChamberCDoorState => "Open";
    public double VisualThetaAngle => -180;
    public string ThetaTargetName => "Home / Start";
    public long PreservedThetaEncoderValue => 0;
    public string ZState => "Z Safe";
    public bool IsBladeExtended => false;
    public bool IsCylinderForward => false;
    public bool IsCylinderBackward => true;
    public bool IsVacuumOn => false;
    public bool IsWaferOnBlade => false;
    public bool IsWaferInFoupA1 => true;
    public bool IsWaferInChamberA => false;
    public bool IsWaferInChamberB => false;
    public bool IsWaferInChamberC => false;
    public bool IsWaferInFoupB1 => false;
    public bool ChamberADoorOpen => false;
    public bool ChamberBDoorOpen => false;
    public bool ChamberCDoorOpen => true;
    public bool TowerRed => false;
    public bool TowerYellow => false;
    public bool TowerGreen => false;
    public string AlarmSummary => "Designer preview: no active alarms";
    public string SelectedSequenceSpeed { get; set; } = "Normal";
    public string PipelineState => "Ready";
    public int FoupACount => 5;
    public int FoupBCount => 0;
    public int CompletedCount => 0;
    public string ActiveStationKey => "Home";
    public int ActiveSlotLevel => 0;
    public string FoupASlotMask => "11111";
    public string FoupBSlotMask => "00000";
    public string CurrentTransferDescription => "Ready";
    public string ActiveWaferId => string.Empty;
    public string WaferIdOnBlade => string.Empty;
    public string TimingProfileName => "Normal";
    public string ModeLabel => "SIMULATOR";
    public string ConnectionLabel => "Designer";
    public double BladeLength => 92;
    public double BladeScaleY => 0.38;
    public string CylinderState => "Retracted / Cylinder backward";
    public string VacuumState => "Vacuum OFF";
    public string WaferSummary => "5/5 in FOUP A, 0/5 in FOUP B";
    public string FoupASummary => "FOUP A: 5/5";
    public string FoupBSummary => "FOUP B: 0/5";

    public ICommand RunTransferSequenceCommand { get; }
    public ICommand PauseCommand { get; }
    public ICommand ResumeCommand { get; }
    public ICommand StepOnceCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand AutoStartCommand { get; }
    public ICommand AutoStopCommand { get; }
    public ICommand EmergencyStopCommand { get; }

    private static RelayCommand CreateNoOpCommand() => new(_ => { });
}
