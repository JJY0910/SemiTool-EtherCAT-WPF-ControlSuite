namespace SemiTool.Domain;

/// <summary>
/// Shared EtherCAT controller contract used by the application layer, simulator, and real hardware adapter.
/// </summary>
/// <remarks>
/// Keeping this abstraction in the domain project lets simulator, designer, and capture paths run without loading the
/// real hardware assembly. The IEG3268-backed implementation still lives in <c>SemiTool.Hardware</c> and is loaded only
/// by the explicit RealHardware startup path.
/// </remarks>
public interface IEthercatController
{
    bool IsConnected { get; }

    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    Task<bool> ReadDigitalInputAsync(IoPoint point, CancellationToken cancellationToken = default);
    Task WriteDigitalOutputAsync(IoPoint point, bool value, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<IoPoint, bool>> ReadAllInputsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<IoPoint, bool>> ReadAllOutputsAsync(CancellationToken cancellationToken = default);
    Task ServoOnAsync(CancellationToken cancellationToken = default);
    Task ServoOffAsync(CancellationToken cancellationToken = default);
    Task HomeAxisAsync(AxisId axis, CancellationToken cancellationToken = default);
    Task MoveAxisAbsoluteAsync(AxisId axis, long targetPosition, CancellationToken cancellationToken = default);
    Task<long> ReadAxisPositionAsync(AxisId axis, CancellationToken cancellationToken = default);
    Task StopMotionAsync(CancellationToken cancellationToken = default);
    Task EmergencyStopAsync(CancellationToken cancellationToken = default);
    Task ResetAlarmAsync(CancellationToken cancellationToken = default);
}
