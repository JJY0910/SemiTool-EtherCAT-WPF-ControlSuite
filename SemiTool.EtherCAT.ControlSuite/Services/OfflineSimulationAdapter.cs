using SemiTool.EtherCAT.ControlSuite.Models;

namespace SemiTool.EtherCAT.ControlSuite.Services;

public sealed class OfflineSimulationAdapter : IEquipmentAdapter
{
    private readonly CommandGate _commandGate;
    private readonly OfflineEquipmentSimulator _simulator;
    private EquipmentSnapshot _snapshot;

    public OfflineSimulationAdapter(OfflineEquipmentSimulator simulator, CommandGate commandGate)
    {
        _simulator = simulator;
        _commandGate = commandGate;
        _snapshot = _simulator.CreatePowerOnSnapshot();
        ConnectionState = BuildState(isConnected: false, "오프라인 시뮬레이터 초기화");
    }

    public AdapterConnectionState ConnectionState { get; private set; }

    public EquipmentSnapshot ReadSnapshot()
    {
        return _snapshot;
    }

    public CommandDecision EvaluateCommand(EquipmentCommand command, bool approvedTeachingLoaded)
    {
        return _commandGate.Evaluate(command, _snapshot, approvedTeachingLoaded);
    }

    public EquipmentSnapshot Connect()
    {
        _snapshot = _simulator.ConnectOfflineRig();
        ConnectionState = BuildState(isConnected: true, "오프라인 시뮬레이터 연결");
        return _snapshot;
    }

    public EquipmentSnapshot ExecuteOfflineStep(string route)
    {
        _snapshot = _simulator.AdvanceCycle(route);
        ConnectionState = BuildState(isConnected: true, "오프라인 시뮬레이션 스텝 실행");
        return _snapshot;
    }

    private static AdapterConnectionState BuildState(bool isConnected, string message)
    {
        return new AdapterConnectionState(
            "Offline Simulation Adapter",
            isConnected,
            IsSimulation: true,
            Endpoint: "local-simulator",
            DateTimeOffset.Now,
            message);
    }
}
