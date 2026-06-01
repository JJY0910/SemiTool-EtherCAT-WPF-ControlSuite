namespace SemiTool.EtherCAT.ControlSuite.Models;

public sealed record CommandAuditRecord(
    DateTimeOffset Timestamp,
    EquipmentCommandType CommandType,
    string Route,
    bool Allowed,
    string Reason,
    string RequestedBy)
{
    public string TimeText => Timestamp.ToLocalTime().ToString("HH:mm:ss");

    public string ResultText => Allowed ? "ALLOW" : "BLOCK";
}
