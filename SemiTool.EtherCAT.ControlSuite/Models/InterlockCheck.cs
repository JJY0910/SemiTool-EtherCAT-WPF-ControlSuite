namespace SemiTool.EtherCAT.ControlSuite.Models;

public sealed record InterlockCheck(
    string Name,
    string Source,
    string Detail,
    bool IsSatisfied,
    InterlockSeverity Severity);
