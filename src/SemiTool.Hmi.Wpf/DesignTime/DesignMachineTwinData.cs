using System.Collections.ObjectModel;
using System.IO;

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
        "[designer] Runtime motion is verified by Run Teaching Demo, not by the static designer."
    ];

    public static string ResolveReferencePhotoPath()
    {
        foreach (var candidate in EnumerateReferencePhotoCandidates())
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        // Designer fallback: the project file links this content item as Assets/...
        // so Visual Studio can still resolve the image when the repo root walk is
        // unavailable in design mode.
        return Path.Combine("Assets", "real-equipment-context-top-view.jpg");
    }

    private static IEnumerable<string> EnumerateReferencePhotoCandidates()
    {
        yield return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "Assets", "real-equipment-context-top-view.jpg"));
        yield return Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "docs", "images", "real-equipment-context-top-view.jpg"));

        var directory = new DirectoryInfo(Environment.CurrentDirectory);
        while (directory is not null)
        {
            var solutionPath = Path.Combine(directory.FullName, "SemiTool.EtherCAT.WPF.ControlSuite.sln");
            if (File.Exists(solutionPath))
            {
                yield return Path.Combine(directory.FullName, "docs", "images", "real-equipment-context-top-view.jpg");
            }

            directory = directory.Parent;
        }
    }
}

public sealed record DesignFoupSlotChipViewModel(
    string Label,
    bool HasWafer,
    string WaferId,
    string State,
    bool IsActive);

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
