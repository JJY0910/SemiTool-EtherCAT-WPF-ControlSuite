using SemiTool.EtherCAT.ControlSuite.Models;

namespace SemiTool.EtherCAT.ControlSuite.Services;

public sealed class OfflineScenarioRunner
{
    private readonly CommandGate _commandGate;

    public OfflineScenarioRunner(CommandGate commandGate)
    {
        _commandGate = commandGate;
    }

    public ScenarioValidationReport RunNominalTransfer(string route)
    {
        var simulator = new OfflineEquipmentSimulator();
        var findings = new List<string>();
        var stepsRun = 0;

        var snapshot = simulator.ConnectOfflineRig();
        stepsRun++;

        var command = EquipmentCommand.Create(EquipmentCommandType.AdvanceOfflineSimulation, route, "ScenarioRunner");
        snapshot = simulator.VerifySlotMap();
        stepsRun++;

        var decision = _commandGate.Evaluate(command, snapshot, approvedTeachingLoaded: false);
        if (!decision.IsAllowed)
        {
            findings.Add($"Initial command blocked: {decision.Reason}");
        }

        while (snapshot.SequenceProgress < 100 && findings.Count == 0)
        {
            decision = _commandGate.Evaluate(command, snapshot, approvedTeachingLoaded: false);
            if (!decision.IsAllowed)
            {
                findings.Add($"Step blocked at {snapshot.SequenceProgress}%: {decision.Reason}");
                break;
            }

            snapshot = simulator.AdvanceCycle(route);
            stepsRun++;
        }

        if (snapshot.SequenceProgress != 100)
        {
            findings.Add($"Scenario ended at {snapshot.SequenceProgress}% instead of 100%.");
        }

        return new ScenarioValidationReport(
            "Nominal Offline Transfer",
            findings.Count == 0,
            stepsRun,
            findings.Count == 0 ? "Offline transfer scenario completed." : "Offline transfer scenario failed.",
            findings);
    }

    public ScenarioValidationReport RunDoorOpenBlock(string route)
    {
        var simulator = new OfflineEquipmentSimulator();
        var findings = new List<string>();

        var snapshot = simulator.ConnectOfflineRig();
        snapshot = simulator.SetChamberDoorOpen(isOpen: true);
        var command = EquipmentCommand.Create(EquipmentCommandType.AdvanceOfflineSimulation, route, "ScenarioRunner");
        var decision = _commandGate.Evaluate(command, snapshot, approvedTeachingLoaded: false);

        if (decision.IsAllowed)
        {
            findings.Add("Door-open scenario allowed motion when it should block.");
        }

        return new ScenarioValidationReport(
            "Door Open Block",
            findings.Count == 0,
            StepsRun: 2,
            findings.Count == 0 ? "Door-open block scenario passed." : "Door-open block scenario failed.",
            findings);
    }
}
