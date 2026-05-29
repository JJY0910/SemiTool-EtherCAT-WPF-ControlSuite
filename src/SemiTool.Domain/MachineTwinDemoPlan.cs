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
    public static IReadOnlyList<MachineTwinDemoStep> CreateDefault(DigitalTwinPhysicalModel model)
    {
        var stationByKey = model.ThetaSwing.Stations.ToDictionary(station => station.PoseKey, StringComparer.OrdinalIgnoreCase);

        // This local factory keeps each demo step explicit while ensuring every
        // trace row carries both the preserved machine encoder value and the
        // separate limited-swing visual angle used by the WPF Canvas.
        MachineTwinDemoStep Step(
            int index,
            string name,
            string stationKey,
            string previous,
            string next,
            string currentStep,
            string zState,
            bool bladeExtended,
            bool cylinderForward,
            bool cylinderBackward,
            bool vacuumOn,
            bool waferOnBlade,
            bool foupA,
            bool chamberA,
            bool chamberB,
            bool chamberC,
            bool foupB,
            bool doorA,
            bool doorB,
            bool doorC,
            bool towerGreen,
            string eventMessage)
        {
            var station = stationByKey[stationKey];
            return new MachineTwinDemoStep(
                index,
                name,
                stationKey,
                station.DisplayName,
                previous,
                next,
                currentStep,
                station.ThetaEncoderPosition,
                station.VisualArcPositionDegrees,
                zState,
                bladeExtended,
                cylinderForward,
                cylinderBackward,
                vacuumOn,
                waferOnBlade,
                foupA,
                chamberA,
                chamberB,
                chamberC,
                foupB,
                doorA,
                doorB,
                doorC,
                towerGreen,
                eventMessage);
        }

        // The list intentionally includes the capture/report milestones, not
        // every possible low-level motion command. The WPF view interpolates
        // these safe simulator states without touching real hardware.
        return
        [
            Step(0, "Startup Simulator", "FoupA", "-", "FOUP A", "Simulator startup / safe state", "Z Safe", false, false, true, false, false, true, false, false, false, false, false, false, false, false, "Startup simulator state. No real hardware connected."),
            Step(1, "Initial FOUP A Slot 1", "FoupA", "-", "FOUP A", "FOUP A Slot 1 contains wafer", "Z Safe", false, false, true, false, false, true, false, false, false, false, false, false, false, false, "Wafer A01 is ready in FOUP A Slot 1."),
            Step(2, "Theta To FOUP A", "FoupA", "-", "Chamber A", "Theta target FOUP A", "Z Safe", false, false, true, false, false, true, false, false, false, false, false, false, false, false, "Limited theta swing targets FOUP A."),
            Step(3, "Z Work / Blade Extend", "FoupA", "-", "Chamber A", "Z Work and blade extended into FOUP A", "Z Work", true, true, false, false, false, true, false, false, false, false, false, false, false, false, "CylinderForward extends the telescopic blade."),
            Step(4, "Vacuum Suction / Wafer On Blade", "FoupA", "-", "Chamber A", "Vacuum suction holds wafer", "Z Work", true, true, false, true, true, false, false, false, false, false, false, false, false, false, "VacuumSuction holds wafer A01 on the blade."),
            Step(5, "Transfer To Chamber A", "ChamberA", "FOUP A", "Chamber A", "Swing toward Chamber A", "Z Safe", false, false, true, true, true, false, false, false, false, false, true, false, false, false, "Blade retracts and theta swings to Chamber A."),
            Step(6, "Place Chamber A", "ChamberA", "FOUP A", "Chamber B", "Release wafer into Chamber A / PreClean starts", "Z Work", true, true, false, false, false, false, true, false, false, false, false, false, false, false, "VacuumExhaust releases wafer into Chamber A."),
            Step(7, "Transfer Chamber A To B", "ChamberB", "Chamber A", "Chamber B", "Move wafer to Chamber B CMP_Main", "Z Work", true, true, false, false, false, false, false, true, false, false, false, true, false, false, "Chamber B CMP_Main process starts."),
            Step(8, "Transfer Chamber B To C", "ChamberC", "Chamber B", "Chamber C", "Move wafer to Chamber C PostClean_Dry", "Z Work", true, true, false, false, false, false, false, false, true, false, false, false, true, false, "Chamber C PostClean_Dry process starts."),
            Step(9, "Transfer Chamber C To FOUP B", "FoupB", "Chamber C", "FOUP B", "Place wafer into FOUP B Slot 1", "Z Work", true, true, false, false, false, false, false, false, false, true, false, false, false, false, "Wafer A01 is placed into FOUP B Slot 1."),
            Step(10, "Process Complete Green Blink", "FoupB", "Chamber C", "-", "Overall simulator flow complete", "Z Safe", false, false, true, false, false, false, false, false, false, true, false, false, false, true, "Tower green indicates simulator flow complete."),
            Step(11, "Reset Safe State", "FoupA", "-", "FOUP A", "Reset to safe simulator state", "Z Safe", false, false, true, false, false, true, false, false, false, false, false, false, false, false, "Reset returns the simulator display to a safe state.")
        ];
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
    string EventLogMessage);
