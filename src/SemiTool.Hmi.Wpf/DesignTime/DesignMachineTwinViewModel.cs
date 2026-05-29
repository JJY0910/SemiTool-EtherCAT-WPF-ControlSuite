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
            "W04",
            "Processing",
            "PreClean_Default",
            "Chem Clean",
            6,
            55,
            false);

        ChamberB = new DesignChamberPipelineViewModel(
            "Chamber B",
            "CMP Main",
            true,
            "W03",
            "Processing",
            "CMP_Main",
            "Bulk Polish",
            12,
            60,
            false);

        ChamberC = new DesignChamberPipelineViewModel(
            "Chamber C",
            "Post-Clean & Dry",
            true,
            "W02",
            "Processing",
            "PostClean_Dry",
            "Dry Rinse",
            4,
            70,
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
    public string FeedbackBoundary => "Design-time sample only. Five unique wafers total; runtime demo animates the real ViewModel.";
    public bool IsDemoRunning => false;
    public bool IsSimulatorMode => true;
    public bool IsRealHardwareMode => false;
    public bool IsConnected => false;
    public string MachineState => "DesignerPreview";
    public string CurrentStation => "Chamber C";
    public string PreviousStation => "Chamber B";
    public string NextStation => "FOUP B";
    public string CurrentStepName => "Designer sample: five unique wafers / pipeline mid-drain";
    public double VisualThetaAngle => 75;
    public string ThetaTargetName => "Chamber C";
    public long PreservedThetaEncoderValue => -322000;
    public string ZState => "Z Work";
    public bool IsBladeExtended => true;
    public bool IsCylinderForward => true;
    public bool IsCylinderBackward => false;
    public bool IsVacuumOn => false;
    public bool IsWaferOnBlade => false;
    public bool IsWaferInFoupA1 => true;
    public bool IsWaferInChamberA => true;
    public bool IsWaferInChamberB => true;
    public bool IsWaferInChamberC => true;
    public bool IsWaferInFoupB1 => true;
    public bool ChamberADoorOpen => false;
    public bool ChamberBDoorOpen => false;
    public bool ChamberCDoorOpen => false;
    public bool TowerRed => false;
    public bool TowerYellow => false;
    public bool TowerGreen => true;
    public string AlarmSummary => "Designer preview: no active alarms";
    public string SelectedDemoSpeed { get; set; } = "Realistic";
    public string PipelineState => "Running";
    public int FoupACount => 1;
    public int FoupBCount => 1;
    public int CompletedCount => 1;
    public string CurrentTransferDescription => "Static preview: W01 complete, W02-W04 in chambers, W05 waiting.";
    public string ActiveWaferId => "W02";
    public string WaferIdOnBlade => string.Empty;
    public string TimingProfileName => "Realistic";
    public string ModeLabel => "SIMULATOR";
    public string ConnectionLabel => "Designer";
    public double BladeLength => 245;
    public double BladeScaleY => 1.0;
    public string CylinderState => "Forward / blade extended";
    public string VacuumState => "Vacuum OFF / no wafer on blade";
    public string WaferSummary => "5 unique wafers: W01 B1, W02 C, W03 B, W04 A, W05 FOUP A";
    public string FoupASummary => "FOUP A: 1/5 waiting";
    public string FoupBSummary => "FOUP B: 1/5 completed";

    public ICommand RunSimulatorDemoCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand AutoStartCommand { get; }
    public ICommand AutoStopCommand { get; }
    public ICommand EmergencyStopCommand { get; }

    private static RelayCommand CreateNoOpCommand() => new(_ => { });
}
