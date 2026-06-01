using System.Collections.ObjectModel;
namespace SemiTool.Hmi.Wpf.DesignTime;

/// <summary>
/// Static sample data used only by the Visual Studio XAML Designer.
/// </summary>
/// <remarks>
/// The design-time model must stay independent from RuntimeCoordinator, the
/// EtherCAT controller, and IEG3268_Dll.dll. It exists so MainWindow.xaml and
/// MachineTwinView.xaml show a meaningful equipment HMI preview without running
/// the app or touching real hardware.
/// </remarks>
public static class DesignMachineTwinData
{
    public static ObservableCollection<DesignFoupSlotChipViewModel> CreateFoupASlots() =>
    [
        new("A1", false, string.Empty, "Empty", false),
        new("A2", false, string.Empty, "Empty", false),
        new("A3", false, string.Empty, "Empty", false),
        new("A4", false, string.Empty, "Empty", false),
        new("A5", true, "W05", "Waiting", true)
    ];

    public static ObservableCollection<DesignFoupSlotChipViewModel> CreateFoupBSlots() =>
    [
        new("B1", true, "W01", "Completed", false),
        new("B2", false, string.Empty, "Empty", false),
        new("B3", false, string.Empty, "Empty", false),
        new("B4", false, string.Empty, "Empty", false),
        new("B5", false, string.Empty, "Empty", false)
    ];

    public static ObservableCollection<DesignMachineTwinStationViewModel> CreateStations() =>
    [
        new("FOUP A", "Source cassette", 14140, -150, false),
        new("Chamber A", "Pre-Clean", -59064, -75, true),
        new("Chamber B (CMP)", "CMP Main", -190823, 0, false),
        new("Chamber C", "Post-Clean & Dry", -322000, 75, false),
        new("FOUP B", "Destination cassette", -394293, 150, false)
    ];

    public static ObservableCollection<string> CreateEventLogLines() =>
    [
        "[designer] Static five-wafer preview loaded. No real hardware connection.",
        "[designer] Exactly five unique wafers are shown: W01-W05.",
        "[designer] W01 is completed in FOUP B; W02/W03/W04 are in Chamber C/B/A.",
        "[designer] W05 remains waiting in FOUP A; blade is shown without duplicating a wafer.",
        "[designer] Runtime motion is verified by Run Transfer Sequence, not by the static designer."
    ];

}

public sealed record DesignFoupSlotChipViewModel(
    string Label,
    bool HasWafer,
    string WaferId,
    string State,
    bool IsActive)
{
    public string SlotDisplay => HasWafer ? WaferId : "Empty";
}

public sealed record DesignMachineTwinStationViewModel(
    string DisplayName,
    string Role,
    long ThetaEncoderPosition,
    double VisualArcPositionDegrees,
    bool IsCurrent);

public sealed record DesignChamberPipelineViewModel(
    string ChamberName,
    string Role,
    bool HasWafer,
    string WaferId,
    string ProcessState,
    string RecipeName,
    string CurrentStep,
    int RemainingTime,
    double ProgressPercent,
    bool DoorOpen)
{
    public string Summary => HasWafer ? $"{WaferId} / {ProcessState}" : "Empty";
}
