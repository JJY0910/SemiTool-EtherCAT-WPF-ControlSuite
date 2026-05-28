using SemiTool.Domain;

namespace SemiTool.Hardware;

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
