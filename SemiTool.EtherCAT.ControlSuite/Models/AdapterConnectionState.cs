namespace SemiTool.EtherCAT.ControlSuite.Models;

public sealed record AdapterConnectionState(
    string AdapterName,
    bool IsConnected,
    bool IsSimulation,
    string Endpoint,
    DateTimeOffset LastUpdated,
    string Message);
