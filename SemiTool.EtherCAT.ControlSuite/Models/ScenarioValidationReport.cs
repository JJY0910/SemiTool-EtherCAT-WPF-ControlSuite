namespace SemiTool.EtherCAT.ControlSuite.Models;

public sealed record ScenarioValidationReport(
    string Name,
    bool Passed,
    int StepsRun,
    string Summary,
    IReadOnlyList<string> Findings);
