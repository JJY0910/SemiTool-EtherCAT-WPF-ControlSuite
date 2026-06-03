namespace SemiTool.Domain;

/// <summary>
/// Simulator-only timing profile for the Machine Twin transfer sequence.
/// </summary>
/// <remarks>
/// These values are not hardware constants. They only control how quickly the
/// WPF simulator moves through visible phases. Capture mode can use a fast
/// profile, while normal runtime defaults to realistic non-instant timing.
/// </remarks>
public sealed record SimulatorTimingProfile(
    string Name,
    int ThetaSwingMs,
    int ZSafeToWorkMs,
    int ZWorkToSafeMs,
    int BladeExtendMs,
    int BladeRetractMs,
    int VacuumSuctionSettleMs,
    int VacuumExhaustSettleMs,
    int DoorOpenCloseMs,
    int ChamberAProcessSeconds,
    int ChamberBProcessSeconds,
    int ChamberCProcessSeconds,
    int TowerBlinkMs)
{
    public static SimulatorTimingProfile Normal { get; } = new(
        "Normal",
        1000,
        750,
        750,
        1000,
        1000,
        750,
        750,
        1200,
        4,
        5,
        4,
        500);

    public static SimulatorTimingProfile Realistic { get; } = new(
        "Realistic",
        1800,
        900,
        900,
        1100,
        1100,
        900,
        900,
        1100,
        8,
        10,
        8,
        500);

    public static SimulatorTimingProfile Fast { get; } = new(
        "Fast",
        260,
        160,
        160,
        180,
        180,
        140,
        140,
        180,
        2,
        3,
        2,
        220);

    public bool IsInstant =>
        ThetaSwingMs <= 0 ||
        ZSafeToWorkMs <= 0 ||
        ZWorkToSafeMs <= 0 ||
        BladeExtendMs <= 0 ||
        BladeRetractMs <= 0 ||
        VacuumSuctionSettleMs <= 0 ||
        VacuumExhaustSettleMs <= 0 ||
        ChamberAProcessSeconds <= 0 ||
        ChamberBProcessSeconds <= 0 ||
        ChamberCProcessSeconds <= 0;
}

public enum PipelineStateKind
{
    Ready,
    Running,
    Paused,
    Stopped,
    Completed,
    Alarm
}

public enum WaferTransferPriority
{
    ProcessCountdown,
    FoupAToChamberA,
    ChamberAToChamberB,
    ChamberBToChamberC,
    ChamberCToFoupB
}

public enum ChamberDoorSequenceState
{
    Closed,
    Opening,
    Open,
    Closing,
    Unknown,
    Fault
}

public enum BladeSequenceState
{
    Retracted,
    Extending,
    Extended,
    Retracting
}

public enum VacuumSequenceState
{
    Off,
    SuctionOn,
    ExhaustOrRelease
}

public enum RobotSequenceState
{
    Idle,
    MovingTheta,
    MovingZ,
    Picking,
    Placing
}

/// <summary>
/// One FOUP cassette slot in the simulator pipeline.
/// </summary>
public sealed record WaferPipelineSlot(
    string SlotName,
    bool HasWafer,
    string WaferId,
    string State,
    bool IsActive);

/// <summary>
/// One chamber's process state for the simulator Machine Twin.
/// </summary>
public sealed record ChamberPipelineSnapshot(
    string ChamberName,
    string Role,
    bool HasWafer,
    string WaferId,
    string ProcessState,
    string RecipeName,
    string CurrentStep,
    int RemainingSeconds,
    double ProgressPercent,
    bool DoorOpen);

/// <summary>
/// Complete visual state for a 5-wafer simulator pipeline milestone.
/// </summary>
public sealed record WaferPipelineSnapshot(
    int StepIndex,
    string StepName,
    string ScreenshotName,
    PipelineStateKind PipelineState,
    IReadOnlyList<WaferPipelineSlot> FoupASlots,
    IReadOnlyList<WaferPipelineSlot> FoupBSlots,
    ChamberPipelineSnapshot ChamberA,
    ChamberPipelineSnapshot ChamberB,
    ChamberPipelineSnapshot ChamberC,
    string CurrentStationKey,
    string PreviousStation,
    string NextStation,
    string CurrentStepName,
    string CurrentTransferDescription,
    string CurrentAction,
    string ActiveWaferId,
    RobotSequenceState RobotState,
    BladeSequenceState BladeState,
    VacuumSequenceState VacuumSequenceState,
    ChamberDoorSequenceState ChamberADoorState,
    ChamberDoorSequenceState ChamberBDoorState,
    ChamberDoorSequenceState ChamberCDoorState,
    bool IsMoving,
    string ZState,
    bool IsBladeExtended,
    bool IsCylinderForward,
    bool IsCylinderBackward,
    string VacuumState,
    bool IsWaferOnBlade,
    string WaferIdOnBlade,
    bool TowerGreen,
    int DelayMs,
    string EventLogMessage)
{
    public int TotalWafers => 5;
    public int FoupACount => FoupASlots.Count(slot => slot.HasWafer);
    public int FoupBCount => FoupBSlots.Count(slot => slot.HasWafer);
    public int CompletedCount => FoupBCount;
    public string WaferIds => string.Join(" ", Enumerable.Range(1, TotalWafers).Select(index => $"W{index:00}"));
}

/// <summary>
/// Builds deterministic, simulator-only 5-wafer pipeline states for the runtime Machine Twin.
/// </summary>
/// <remarks>
/// This model deliberately stays separate from EquipmentProfile. It does not
/// change preserved encoder positions, I/O channels, recipes, or timing values
/// used by real hardware logic. Its job is to make the simulator HMI behave
/// like a cassette/chamber pipeline instead of a one-wafer toy animation.
/// </remarks>
public static class WaferPipelineSimulator
{
    public const int TotalWafers = 5;

    public static WaferPipelineSnapshot CreateInitial(SimulatorTimingProfile timing) =>
        Snapshot(
            timing,
            0,
            "Startup Simulator",
            "00-startup-simulator.png",
            PipelineStateKind.Ready,
            ["W01", "W02", "W03", "W04", "W05"],
            [],
            EmptyChamber("Chamber A", "Pre-Clean", "PreClean_Default"),
            EmptyChamber("Chamber B", "CMP Main", "CMP_Main"),
            EmptyChamber("Chamber C", "Post-Clean & Dry", "PostClean_Dry"),
            "Home",
            "-",
            "FOUP A",
            "Pipeline ready: FOUP A 5 wafers, FOUP B empty",
            "Ready",
            string.Empty,
            false,
            "Z Safe",
            false,
            false,
            true,
            "Off",
            false,
            string.Empty,
            false,
            "Pipeline ready: FOUP A 5 wafers, FOUP B empty.");

    public static IReadOnlyList<WaferPipelineSnapshot> CreateDebugTimeline(SimulatorTimingProfile timing) =>
    [
        CreateInitial(timing),
        Snapshot(timing, 1, "FOUP A 5 Wafers", "01-foup-a-5-wafers.png", PipelineStateKind.Ready,
            ["W01", "W02", "W03", "W04", "W05"], [], EmptyA(), EmptyB(), EmptyC(), "FoupA", "-", "FOUP A",
            "A1-A5 loaded / B1-B5 empty", "Ready", string.Empty, false, "Z Safe", false, false, true, "Off", false, string.Empty, false,
            "FOUP A slots A1-A5 are loaded. FOUP B is empty."),
        Snapshot(timing, 2, "W01 Pick A1", "02-w01-pick-a1.png", PipelineStateKind.Running,
            ["", "W02", "W03", "W04", "W05"], [], EmptyA(), EmptyB(), EmptyC(), "FoupA", "-", "Chamber A",
            "Transfer W01: FOUP A Slot A1 -> blade", "FOUP A A1 -> Chamber A", "W01", true, "Z Work", true, true, false, "Off", false, string.Empty, false,
            "Transfer W01: FOUP A Slot A1 -> Chamber A started. Z Work, blade extended."),
        Snapshot(timing, 3, "W01 On Blade", "03-w01-on-blade.png", PipelineStateKind.Running,
            ["", "W02", "W03", "W04", "W05"], [], EmptyA(), EmptyB(), EmptyC(), "FoupA", "FOUP A", "Chamber A",
            "Vacuum suction ON: W01 on blade", "FOUP A A1 -> Chamber A", "W01", true, "Z Work", true, true, false, "Suction", true, "W01", false,
            "Vacuum suction ON: W01 is on the blade."),
        Snapshot(timing, 4, "W01 Chamber A Processing", "04-w01-chamber-a-processing.png", PipelineStateKind.Running,
            ["", "W02", "W03", "W04", "W05"], [], ProcessingA("W01", timing.ChamberAProcessSeconds, 25), EmptyB(), EmptyC(), "ChamberA", "FOUP A", "Chamber B",
            "W01 in Chamber A / PreClean_Default processing", "Chamber A process", "W01", false, "Z Safe", false, false, true, "Off", false, string.Empty, false,
            "W01 placed in Chamber A. PreClean_Default started."),
        Snapshot(timing, 5, "W02 Feeds While W01 Moves To B", "05-w02-enters-chamber-a-while-w01-moves-to-b.png", PipelineStateKind.Running,
            ["", "", "W03", "W04", "W05"], [], ProcessingA("W02", timing.ChamberAProcessSeconds, 20), ProcessingB("W01", timing.ChamberBProcessSeconds, 15), EmptyC(), "ChamberA", "Chamber A", "Chamber B",
            "W02 enters Chamber A while W01 moves to Chamber B", "A -> B, FOUP A -> A", "W02", true, "Z Work", true, true, false, "Exhaust", false, string.Empty, false,
            "Scheduler fed W02 into Chamber A after W01 moved to Chamber B."),
        Snapshot(timing, 6, "Three Chambers Occupied", "06-pipeline-three-chambers-occupied.png", PipelineStateKind.Running,
            ["", "", "", "W04", "W05"], [], ProcessingA("W03", timing.ChamberAProcessSeconds, 15), ProcessingB("W02", timing.ChamberBProcessSeconds, 55), ProcessingC("W01", timing.ChamberCProcessSeconds, 35), "ChamberB", "Chamber A", "Chamber C",
            "Pipeline loaded: A=W03, B=W02, C=W01", "Pipeline processing", "W03", false, "Z Safe", false, false, true, "Off", false, string.Empty, false,
            "Pipeline state: Chamber A W03, Chamber B W02, Chamber C W01."),
        Snapshot(timing, 7, "W01 Chamber C Complete", "07-w01-chamber-c-complete.png", PipelineStateKind.Running,
            ["", "", "", "W04", "W05"], [], ProcessingA("W03", timing.ChamberAProcessSeconds, 45), ProcessingB("W02", timing.ChamberBProcessSeconds, 70), CompletedC("W01"), "ChamberC", "Chamber B", "FOUP B",
            "W01 complete in Chamber C / waiting transfer", "Chamber C -> FOUP B", "W01", false, "Z Safe", false, false, true, "Off", false, string.Empty, false,
            "W01 Chamber C process complete. Scheduler priority selects Chamber C -> FOUP B."),
        Snapshot(timing, 8, "W01 Placed FOUP B B1", "08-w01-placed-foup-b-b1.png", PipelineStateKind.Running,
            ["", "", "", "", "W05"], ["W01"], ProcessingA("W04", timing.ChamberAProcessSeconds, 10), ProcessingB("W03", timing.ChamberBProcessSeconds, 35), ProcessingC("W02", timing.ChamberCProcessSeconds, 20), "FoupB", "Chamber C", "FOUP B",
            "W01 placed into FOUP B Slot B1", "Chamber C -> FOUP B B1", "W01", true, "Z Work", true, true, false, "Exhaust", false, string.Empty, false,
            "W01 placed into FOUP B Slot B1. FOUP B count is now 1/5."),
        Snapshot(timing, 9, "FOUP A Empty Pipeline Finishing", "09-foup-a-empty-pipeline-finishing.png", PipelineStateKind.Running,
            [], ["W01", "W02", "W03"], EmptyA(), CompletedB("W05"), ProcessingC("W04", timing.ChamberCProcessSeconds, 70), "ChamberB", "Chamber A", "Chamber C",
            "FOUP A empty / pipeline draining", "B -> C before new feed", "W05", true, "Z Safe", false, false, true, "Off", false, string.Empty, false,
            "FOUP A is empty. Pipeline is draining remaining wafers toward FOUP B."),
        Snapshot(timing, 10, "FOUP B 5 Wafers Complete", "10-foup-b-5-wafers-complete.png", PipelineStateKind.Completed,
            [], ["W01", "W02", "W03", "W04", "W05"], EmptyA(), EmptyB(), EmptyC(), "FoupB", "Chamber C", "-", "All 5 wafers complete in FOUP B", "Complete", "W05", false, "Z Safe", false, false, true, "Off", false, string.Empty, false,
            "All 5 wafers completed in FOUP B. Tower yellow complete alarm enabled."),
        Snapshot(timing, 11, "Reset Safe State", "11-reset-safe-state.png", PipelineStateKind.Ready,
            ["W01", "W02", "W03", "W04", "W05"], [], EmptyA(), EmptyB(), EmptyC(), "FoupA", "-", "FOUP A", "Reset to safe simulator state", "Reset", string.Empty, false, "Z Safe", false, false, true, "Off", false, string.Empty, false,
            "Reset returns simulator to FOUP A loaded, FOUP B empty, blade retracted, vacuum off.")
    ];

    public static IReadOnlyList<WaferPipelineSnapshot> CreateTransferTimeline(SimulatorTimingProfile timing) =>
        WaferTransferSequence.Create(timing);

    public static WaferTransferPriority ChooseNextTransfer(WaferPipelineSnapshot state)
    {
        if (state.ChamberC.ProcessState == "Completed" && state.FoupBSlots.Any(slot => !slot.HasWafer))
        {
            return WaferTransferPriority.ChamberCToFoupB;
        }

        if (state.ChamberB.ProcessState == "Completed" && !state.ChamberC.HasWafer)
        {
            return WaferTransferPriority.ChamberBToChamberC;
        }

        if (state.ChamberA.ProcessState == "Completed" && !state.ChamberB.HasWafer)
        {
            return WaferTransferPriority.ChamberAToChamberB;
        }

        if (!state.ChamberA.HasWafer && state.FoupASlots.Any(slot => slot.HasWafer))
        {
            return WaferTransferPriority.FoupAToChamberA;
        }

        return WaferTransferPriority.ProcessCountdown;
    }

    private static WaferPipelineSnapshot Snapshot(
        SimulatorTimingProfile timing,
        int index,
        string name,
        string screenshot,
        PipelineStateKind pipelineState,
        IReadOnlyList<string> foupAWafers,
        IReadOnlyList<string> foupBWafers,
        ChamberPipelineSnapshot chamberA,
        ChamberPipelineSnapshot chamberB,
        ChamberPipelineSnapshot chamberC,
        string stationKey,
        string previous,
        string next,
        string stepName,
        string transferDescription,
        string activeWafer,
        bool moving,
        string zState,
        bool bladeExtended,
        bool cylinderForward,
        bool cylinderBackward,
        string vacuumState,
        bool waferOnBlade,
        string waferIdOnBlade,
        bool towerGreen,
        string eventMessage) =>
        new(
            index,
            name,
            screenshot,
            pipelineState,
            BuildSlots("A", foupAWafers, completed: false, activeWafer),
            BuildSlots("B", foupBWafers, completed: true, activeWafer),
            chamberA,
            chamberB,
            chamberC,
            stationKey,
            previous,
            next,
            stepName,
            transferDescription,
            stepName,
            activeWafer,
            moving ? RobotSequenceState.MovingTheta : RobotSequenceState.Idle,
            bladeExtended ? BladeSequenceState.Extended : BladeSequenceState.Retracted,
            vacuumState switch
            {
                "Suction" => VacuumSequenceState.SuctionOn,
                "Exhaust" => VacuumSequenceState.ExhaustOrRelease,
                _ => VacuumSequenceState.Off
            },
            chamberA.DoorOpen ? ChamberDoorSequenceState.Open : ChamberDoorSequenceState.Closed,
            chamberB.DoorOpen ? ChamberDoorSequenceState.Open : ChamberDoorSequenceState.Closed,
            chamberC.DoorOpen ? ChamberDoorSequenceState.Open : ChamberDoorSequenceState.Closed,
            moving,
            zState,
            bladeExtended,
            cylinderForward,
            cylinderBackward,
            vacuumState,
            waferOnBlade,
            waferIdOnBlade,
            towerGreen,
            DelayFor(name, timing),
            eventMessage);

    private static IReadOnlyList<WaferPipelineSlot> BuildSlots(string prefix, IReadOnlyList<string> wafers, bool completed, string activeWafer)
    {
        var slots = new List<WaferPipelineSlot>(TotalWafers);
        for (var i = 1; i <= TotalWafers; i++)
        {
            var waferId = i <= wafers.Count ? wafers[i - 1] : string.Empty;
            var hasWafer = !string.IsNullOrWhiteSpace(waferId);
            var state = hasWafer ? completed ? "Completed" : "Waiting" : "Empty";
            slots.Add(new WaferPipelineSlot($"{prefix}{i}", hasWafer, waferId, state, waferId == activeWafer));
        }

        return slots;
    }

    private static int DelayFor(string stepName, SimulatorTimingProfile timing)
    {
        if (stepName.Contains("Pick", StringComparison.OrdinalIgnoreCase))
        {
            return timing.ZSafeToWorkMs + timing.BladeExtendMs;
        }

        if (stepName.Contains("Blade", StringComparison.OrdinalIgnoreCase))
        {
            return timing.VacuumSuctionSettleMs;
        }

        if (stepName.Contains("Complete", StringComparison.OrdinalIgnoreCase))
        {
            return timing.TowerBlinkMs * 2;
        }

        if (stepName.Contains("Processing", StringComparison.OrdinalIgnoreCase))
        {
            return 1200;
        }

        return timing.ThetaSwingMs;
    }

    private static ChamberPipelineSnapshot EmptyA() => EmptyChamber("Chamber A", "Pre-Clean", "PreClean_Default");
    private static ChamberPipelineSnapshot EmptyB() => EmptyChamber("Chamber B", "CMP Main", "CMP_Main");
    private static ChamberPipelineSnapshot EmptyC() => EmptyChamber("Chamber C", "Post-Clean & Dry", "PostClean_Dry");

    private static ChamberPipelineSnapshot EmptyChamber(string name, string role, string recipe) =>
        new(name, role, false, string.Empty, "Empty", recipe, "-", 0, 0, false);

    private static ChamberPipelineSnapshot ProcessingA(string waferId, int remaining, double progress) =>
        new("Chamber A", "Pre-Clean", true, waferId, "Processing", "PreClean_Default", "Chem Clean", remaining, progress, false);

    private static ChamberPipelineSnapshot ProcessingB(string waferId, int remaining, double progress) =>
        new("Chamber B", "CMP Main", true, waferId, "Processing", "CMP_Main", "Bulk Polish", remaining, progress, false);

    private static ChamberPipelineSnapshot ProcessingC(string waferId, int remaining, double progress) =>
        new("Chamber C", "Post-Clean & Dry", true, waferId, "Processing", "PostClean_Dry", "Spin Dry", remaining, progress, false);

    private static ChamberPipelineSnapshot CompletedB(string waferId) =>
        new("Chamber B", "CMP Main", true, waferId, "Completed", "CMP_Main", "Bulk Polish complete", 0, 100, false);

    private static ChamberPipelineSnapshot CompletedC(string waferId) =>
        new("Chamber C", "Post-Clean & Dry", true, waferId, "Completed", "PostClean_Dry", "Spin Dry complete", 0, 100, false);
}
