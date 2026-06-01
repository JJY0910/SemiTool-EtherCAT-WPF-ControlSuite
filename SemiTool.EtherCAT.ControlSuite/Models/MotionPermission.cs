namespace SemiTool.EtherCAT.ControlSuite.Models;

public sealed record MotionPermission(
    bool CanRunOfflineSimulation,
    bool CanIssueRealMotion,
    string Reason);
