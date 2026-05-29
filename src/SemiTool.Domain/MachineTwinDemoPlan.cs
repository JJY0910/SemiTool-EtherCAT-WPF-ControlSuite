namespace SemiTool.Domain;

/// <summary>
/// Deterministic simulator-only state plan used by the runtime Machine Twin UI and debug evidence capture.
/// </summary>
/// <remarks>
/// The plan is separate from <see cref="EquipmentProfile"/> because it does not define hardware constants. It maps the
/// preserved station/pose values into readable UI states so the WPF HMI can demonstrate FOUP-to-chamber transfer flow
/// without touching real EtherCAT hardware or loading the vendor DLL.
/// </remarks>
public static class MachineTwinDemoPlan
{
    /// <summary>
    /// Builds the repeatable simulator timeline used by the runtime Machine Twin,
    /// portfolio captures, and debug evidence report.
    /// </summary>
    /// <remarks>
    /// The station order deliberately follows the teaching scenario:
    /// FOUP A -&gt; Chamber A -&gt; Chamber B -&gt; Chamber C -&gt; FOUP B.
    /// The theta encoder values are copied from the physical profile through
    /// <see cref="DigitalTwinPhysicalModel"/> and are never interpreted as
    /// display degrees. The visual angle is a separate HMI-only arc position.
    /// </remarks>
    public static IReadOnlyList<MachineTwinDemoStep> CreateDefault(DigitalTwinPhysicalModel model) =>
        Create(model, SimulatorTimingProfile.Teaching);

    public static IReadOnlyList<MachineTwinDemoStep> Create(DigitalTwinPhysicalModel model, SimulatorTimingProfile timing)
    {
        var stationByKey = model.ThetaSwing.Stations.ToDictionary(station => station.PoseKey, StringComparer.OrdinalIgnoreCase);

        return WaferPipelineSimulator.CreateTeachingTimeline(timing)
            .Select(snapshot =>
            {
                var station = stationByKey[snapshot.CurrentStationKey];
                return new MachineTwinDemoStep(
                    snapshot.StepIndex,
                    snapshot.StepName,
                    snapshot.CurrentStationKey,
                    station.DisplayName,
                    snapshot.PreviousStation,
                    snapshot.NextStation,
                    snapshot.CurrentStepName,
                    station.ThetaEncoderPosition,
                    station.VisualArcPositionDegrees,
                    snapshot.CurrentAction,
                    snapshot.RobotState.ToString(),
                    snapshot.BladeState.ToString(),
                    snapshot.VacuumTeachingState.ToString(),
                    snapshot.ChamberADoorState.ToString(),
                    snapshot.ChamberBDoorState.ToString(),
                    snapshot.ChamberCDoorState.ToString(),
                    snapshot.ZState,
                    snapshot.IsBladeExtended,
                    snapshot.IsCylinderForward,
                    snapshot.IsCylinderBackward,
                    snapshot.VacuumState == "Suction",
                    snapshot.IsWaferOnBlade,
                    snapshot.FoupASlots.First(slot => slot.SlotName == "A1").HasWafer,
                    snapshot.ChamberA.HasWafer,
                    snapshot.ChamberB.HasWafer,
                    snapshot.ChamberC.HasWafer,
                    snapshot.FoupBSlots.First(slot => slot.SlotName == "B1").HasWafer,
                    snapshot.ChamberA.DoorOpen,
                    snapshot.ChamberB.DoorOpen,
                    snapshot.ChamberC.DoorOpen,
                    snapshot.TowerGreen,
                    snapshot.EventLogMessage,
                    snapshot.ScreenshotName,
                    snapshot.PipelineState.ToString(),
                    snapshot.FoupACount,
                    snapshot.FoupBCount,
                    snapshot.CompletedCount,
                    snapshot.TotalWafers,
                    snapshot.CurrentTransferDescription,
                    snapshot.ActiveWaferId,
                    snapshot.WaferIdOnBlade,
                    snapshot.VacuumState,
                    snapshot.WaferIds,
                    timing.Name,
                    snapshot.DelayMs,
                    snapshot.FoupASlots,
                    snapshot.FoupBSlots,
                    snapshot.ChamberA,
                    snapshot.ChamberB,
                    snapshot.ChamberC);
            })
            .ToArray();
    }
}

public sealed record MachineTwinDemoStep(
    int StepIndex,
    string StepName,
    string StationKey,
    string CurrentStation,
    string PreviousStation,
    string NextStation,
    string CurrentStepName,
    long PreservedThetaEncoderValue,
    double VisualThetaAngle,
    string CurrentAction,
    string RobotState,
    string BladeState,
    string VacuumTeachingState,
    string ChamberADoorState,
    string ChamberBDoorState,
    string ChamberCDoorState,
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
    bool TowerGreen,
    string EventLogMessage,
    string ScreenshotName,
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
    int DelayMs,
    IReadOnlyList<WaferPipelineSlot> FoupASlots,
    IReadOnlyList<WaferPipelineSlot> FoupBSlots,
    ChamberPipelineSnapshot ChamberA,
    ChamberPipelineSnapshot ChamberB,
    ChamberPipelineSnapshot ChamberC);
