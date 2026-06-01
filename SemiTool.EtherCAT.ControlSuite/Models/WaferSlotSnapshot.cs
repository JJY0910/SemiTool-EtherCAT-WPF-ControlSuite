namespace SemiTool.EtherCAT.ControlSuite.Models;

public sealed record WaferSlotSnapshot(
    string FoupName,
    int SlotNumber,
    WaferSlotState State,
    string WaferId,
    bool Verified)
{
    public string SlotLabel => $"{FoupName}-{SlotNumber:00}";
}
