namespace SemiTool.EtherCAT.ControlSuite.Models;

public sealed record EquipmentCommand(
    EquipmentCommandType Type,
    string Route,
    string RequestedBy,
    DateTimeOffset RequestedAt)
{
    public static EquipmentCommand Create(EquipmentCommandType type, string route, string requestedBy)
    {
        return new EquipmentCommand(type, route, requestedBy, DateTimeOffset.Now);
    }
}
