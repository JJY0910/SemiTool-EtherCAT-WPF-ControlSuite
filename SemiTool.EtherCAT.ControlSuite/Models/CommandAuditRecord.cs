namespace SemiTool.EtherCAT.ControlSuite.Models;

public sealed record CommandAuditRecord(
    DateTimeOffset Timestamp,
    EquipmentCommandType CommandType,
    string Route,
    bool Allowed,
    string Reason,
    string RequestedBy);
