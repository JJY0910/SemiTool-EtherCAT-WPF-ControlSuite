namespace SemiTool.EtherCAT.ControlSuite.Models;

public enum TransferPhase
{
    HomeReady,
    RotateToSource,
    ExtendToSource,
    VacuumPickup,
    RetractFromSource,
    RotateToDestination,
    ExtendToDestination,
    ReleaseAtDestination,
    RetractToHome,
    Complete,
    Blocked
}
