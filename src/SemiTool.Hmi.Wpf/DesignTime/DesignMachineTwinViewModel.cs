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
        ReferencePhotoPath = DesignMachineTwinData.ResolveReferencePhotoPath();
        FoupASlots = DesignMachineTwinData.CreateFoupASlots();
        FoupBSlots = DesignMachineTwinData.CreateFoupBSlots();
        Stations = DesignMachineTwinData.CreateStations();
        EventLogLines = DesignMachineTwinData.CreateEventLogLines();
        DemoSpeedOptions = new ObservableCollection<string>(["Realistic", "Fast", "Step"]);

        ChamberA = new DesignChamberPipelineViewModel(
            "Chamber A",
            "Pre-Clean",
            true,
            "W01",
            "Processing",
            "PreClean_Default",
            "Chem Clean",
            6,
            55,
            true);

        ChamberB = new DesignChamberPipelineViewModel(
            "Chamber B",
            "CMP Main",
            true,
            "W00",
            "Completed",
            "CMP_Main",
            "Bulk Polish complete",
            0,
            100,
            false);

        ChamberC = new DesignChamberPipelineViewModel(
            "Chamber C",
            "Post-Clean & Dry",
            false,
            string.Empty,
            "Empty",
            "PostClean_Dry",
            "-",
            0,
            0,
            false);

        RunSimulatorDemoCommand = CreateNoOpCommand();
        StopCommand = CreateNoOpCommand();
        ResetCommand = CreateNoOpCommand();
        AutoStartCommand = CreateNoOpCommand();
        AutoStopCommand = CreateNoOpCommand();
        EmergencyStopCommand = CreateNoOpCommand();
    }

    public ObservableCollection<DesignMachineTwinStationViewModel> Stations { get; }
    public ObservableCollection<DesignFoupSlotChipViewModel> FoupASlots { get; }
    public ObservableCollection<DesignFoupSlotChipViewModel> FoupBSlots { get; }
    public ObservableCollection<string> DemoSpeedOptions { get; }
    public ObservableCollection<string> EventLogLines { get; }
    public DesignChamberPipelineViewModel ChamberA { get; }
    public DesignChamberPipelineViewModel ChamberB { get; }
    public DesignChamberPipelineViewModel ChamberC { get; }

    public string ReferencePhotoPath { get; }
    public string ScenarioName => "Visual Studio Designer sample";
    public string EquipmentKind => "Wafer transfer robot teaching/training setup";
    public string PhotoCaption => "Real equipment context reference / designer sample data";
    public string FeedbackBoundary => "Design-time simulator sample only. Runtime demo animates the real ViewModel.";
    public bool IsDemoRunning => false;
    public bool IsSimulatorMode => true;
    public bool IsRealHardwareMode => false;
    public bool IsConnected => false;
    public string MachineState => "DesignerPreview";
    public string CurrentStation => "Chamber A";
    public string PreviousStation => "FOUP A";
    public string NextStation => "Chamber B";
    public string CurrentStepName => "Designer sample: W01 on blade / Chamber A processing";
    public double VisualThetaAngle => -75;
    public string ThetaTargetName => "Chamber A";
    public long PreservedThetaEncoderValue => -59064;
    public string ZState => "Z Work";
    public bool IsBladeExtended => true;
    public bool IsCylinderForward => true;
    public bool IsCylinderBackward => false;
    public bool IsVacuumOn => true;
    public bool IsWaferOnBlade => true;
    public bool IsWaferInFoupA1 => false;
    public bool IsWaferInChamberA => true;
    public bool IsWaferInChamberB => true;
    public bool IsWaferInChamberC => false;
    public bool IsWaferInFoupB1 => true;
    public bool ChamberADoorOpen => true;
    public bool ChamberBDoorOpen => false;
    public bool ChamberCDoorOpen => false;
    public bool TowerRed => false;
    public bool TowerYellow => false;
    public bool TowerGreen => true;
    public string AlarmSummary => "Designer preview: no active alarms";
    public string SelectedDemoSpeed { get; set; } = "Realistic";
    public string PipelineState => "Running";
    public int FoupACount => 5;
    public int FoupBCount => 1;
    public int CompletedCount => 1;
    public string CurrentTransferDescription => "Sample pipeline: FOUP A feeds Chamber A while downstream drains first.";
    public string ActiveWaferId => "W01";
    public string WaferIdOnBlade => "W01";
    public string TimingProfileName => "Realistic";
    public string ModeLabel => "SIMULATOR";
    public string ConnectionLabel => "Designer";
    public double BladeLength => 245;
    public double BladeScaleY => 1.0;
    public string CylinderState => "Forward / blade extended";
    public string VacuumState => "Suction ON / wafer held";
    public string WaferSummary => "W01 on blade / B1 completed sample";
    public string FoupASummary => "FOUP A: 5/5 loaded";
    public string FoupBSummary => "FOUP B: 1/5 completed";

    public ICommand RunSimulatorDemoCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand AutoStartCommand { get; }
    public ICommand AutoStopCommand { get; }
    public ICommand EmergencyStopCommand { get; }

    private static RelayCommand CreateNoOpCommand() => new(_ => { });
}
