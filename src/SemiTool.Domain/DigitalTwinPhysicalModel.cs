namespace SemiTool.Domain;

/// <summary>
/// Display-only physical model for the wafer transfer robot sequence monitor.
/// </summary>
/// <remarks>
/// This model is intentionally separate from <see cref="EquipmentProfile"/>.
/// EquipmentProfile preserves hardware channels, timing, and encoder positions, while this type gives the HMI and
/// documentation a safe vocabulary for explaining the physical layout without changing real equipment values.
/// The previous "CMP Cluster" name is kept only as a simulator scenario label.
/// </remarks>
public sealed class DigitalTwinPhysicalModel
{
    public string EquipmentKind { get; init; } = "Wafer transfer robot HMI / sequence monitor";
    public string ScenarioName { get; init; } = "CMP Cluster";
    public string ScenarioMeaning { get; init; } =
        "Previous-year HMI simulator scenario name; not an official claim that the physical equipment is a production CMP cluster tool.";
    public string PhysicalSummary { get; init; } =
        "Fixed aluminum base, central limited-swing theta base, telescopic blade/end-effector, three logical chambers, FOUP A/B, and tower lamp.";
    public ThetaSwingLayout ThetaSwing { get; init; } = new();
    public BladeTransferMechanism BladeMechanism { get; init; } = new();

    public static DigitalTwinPhysicalModel CreateDefault(EquipmentProfile profile) => new()
    {
        ThetaSwing = ThetaSwingLayout.CreateDefault(profile),
        BladeMechanism = BladeTransferMechanism.Default
    };
}

/// <summary>
/// Limited station-to-station theta swing used by the Digital Twin rendering.
/// </summary>
/// <remarks>
/// The real robot is not represented as a continuous 360-degree dial. Preserved theta values are encoder/position
/// targets from the equipment profile; the visual arc positions are HMI display coordinates only.
/// </remarks>
public sealed class ThetaSwingLayout
{
    public bool IsContinuousRotation { get; init; }
    public int VisualSweepApproxDegrees { get; init; } = 300;
    public IReadOnlyList<RobotSwingStation> Stations { get; init; } = Array.Empty<RobotSwingStation>();

    public static ThetaSwingLayout CreateDefault(EquipmentProfile profile) => new()
    {
        IsContinuousRotation = false,
        VisualSweepApproxDegrees = 300,
        Stations =
        [
            new RobotSwingStation(0, "Home", "Home / Start", "Safe startup orientation before approaching a FOUP slot", profile.GetPose("Home").Theta, 0),
            new RobotSwingStation(1, "FoupA", "FOUP A", "Source cassette / lower-left station", profile.GetPose("FoupA").Theta, -150),
            new RobotSwingStation(2, "ChamberA", "Chamber A", "Pre-Clean station / left side", profile.GetPose("ChamberA").Theta, -75),
            new RobotSwingStation(3, "ChamberB", "Chamber B (CMP)", "CMP_Main simulator station / top side", profile.GetPose("ChamberB").Theta, 0),
            new RobotSwingStation(4, "ChamberC", "Chamber C", "Post-Clean & Dry station / right side", profile.GetPose("ChamberC").Theta, 75),
            new RobotSwingStation(5, "FoupB", "FOUP B", "Destination cassette / lower-right station", profile.GetPose("FoupB").Theta, 150)
        ]
    };
}

/// <summary>
/// One detent on the limited theta swing arc.
/// </summary>
public sealed record RobotSwingStation(
    int Order,
    string PoseKey,
    string DisplayName,
    string Role,
    long ThetaEncoderPosition,
    double VisualArcPositionDegrees);

/// <summary>
/// Display metadata for the blade/end-effector that carries the wafer.
/// </summary>
/// <remarks>
/// The blade is shown as a two-stage/telescopic assembly because cylinder forward/backward extends and retracts the
/// wafer-carrying end-effector while theta aims the assembly at each station.
/// </remarks>
public sealed class BladeTransferMechanism
{
    public static BladeTransferMechanism Default { get; } = new();

    public bool IsTelescopic { get; init; } = true;
    public string BaseStage { get; init; } = "Lower/base slide fixed to the rotating theta structure";
    public string FrontBladeStage { get; init; } = "Upper/front blade section extends and retracts";
    public string WaferCarrier { get; init; } = "Blade/end-effector carries the wafer";
    public string ExtendCommand { get; init; } = "CylinderForward";
    public string RetractCommand { get; init; } = "CylinderBackward";
    public string HoldCommand { get; init; } = "VacuumSuction";
    public string ReleaseCommand { get; init; } = "VacuumExhaust";
}
