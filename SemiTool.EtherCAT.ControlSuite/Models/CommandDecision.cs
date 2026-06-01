namespace SemiTool.EtherCAT.ControlSuite.Models;

public sealed record CommandDecision(
    EquipmentCommand Command,
    bool IsAllowed,
    string Reason,
    IReadOnlyList<InterlockCheck> Checks)
{
    public bool HasBlocker => Checks.Any(check => !check.IsSatisfied && check.Severity == InterlockSeverity.Blocker);
}
