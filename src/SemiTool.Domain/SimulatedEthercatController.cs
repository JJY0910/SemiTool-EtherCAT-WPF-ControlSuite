namespace SemiTool.Domain;

/// <summary>
/// Hardware-free EtherCAT simulator used for development, CI, screenshots, and sequence testing.
/// </summary>
/// <remarks>
/// The simulator mirrors the named I/O surface of the real controller but starts with every output OFF. It never loads
/// vendor DLLs and never talks to equipment, which keeps the default startup mode safe on any developer PC.
/// </remarks>
public sealed class SimulatedEthercatController : IEthercatController
{
    private readonly EquipmentProfile _profile;
    private readonly bool _autoCompleteActuators;
    private readonly Dictionary<IoPoint, bool> _inputs = new();
    private readonly Dictionary<IoPoint, bool> _outputs = new();
    private readonly Dictionary<AxisId, long> _axisPositions = new()
    {
        [AxisId.Z] = 0,
        [AxisId.Theta] = 0
    };

    private readonly HashSet<AxisId> _homedAxes = new();
    private bool _servoOn;

    public SimulatedEthercatController(EquipmentProfile profile, bool autoCompleteActuators = true)
    {
        _profile = profile;
        _autoCompleteActuators = autoCompleteActuators;

        foreach (var channel in _profile.GetInputChannels())
        {
            _inputs[channel.Point] = false;
        }

        foreach (var channel in _profile.GetOutputChannels())
        {
            _outputs[channel.Point] = false;
        }
    }

    public bool IsConnected { get; private set; }

    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Keep simulator semantics aligned with the real safety default: connecting must not leave outputs energized.
        TurnOffAllOutputs();
        IsConnected = true;
        return Task.CompletedTask;
    }

    public Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        TurnOffAllOutputs();
        IsConnected = false;
        return Task.CompletedTask;
    }

    public Task<bool> ReadDigitalInputAsync(IoPoint point, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureInput(point);
        return Task.FromResult(_inputs.TryGetValue(point, out var value) && value);
    }

    public Task WriteDigitalOutputAsync(IoPoint point, bool value, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureConnected();
        EnsureOutput(point);

        _outputs[point] = value;
        if (_autoCompleteActuators)
        {
            ApplySimulatedFeedback(point, value);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyDictionary<IoPoint, bool>> ReadAllInputsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyDictionary<IoPoint, bool>>(new Dictionary<IoPoint, bool>(_inputs));
    }

    public Task<IReadOnlyDictionary<IoPoint, bool>> ReadAllOutputsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyDictionary<IoPoint, bool>>(new Dictionary<IoPoint, bool>(_outputs));
    }

    public Task ServoOnAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureConnected();
        _servoOn = true;
        return Task.CompletedTask;
    }

    public Task ServoOffAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _servoOn = false;
        return Task.CompletedTask;
    }

    public Task HomeAxisAsync(AxisId axis, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureConnected();
        EnsureServoOn();
        _axisPositions[axis] = 0;
        _homedAxes.Add(axis);
        return Task.CompletedTask;
    }

    public Task MoveAxisAbsoluteAsync(AxisId axis, long targetPosition, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureConnected();
        EnsureServoOn();
        _axisPositions[axis] = targetPosition;
        return Task.CompletedTask;
    }

    public Task<long> ReadAxisPositionAsync(AxisId axis, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_axisPositions[axis]);
    }

    public Task StopMotionAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public async Task EmergencyStopAsync(CancellationToken cancellationToken = default)
    {
        await StopMotionAsync(cancellationToken).ConfigureAwait(false);
        // Emergency behavior is intentionally conservative even in simulation: outputs and servo state are cleared.
        TurnOffAllOutputs();
        _servoOn = false;
    }

    public Task ResetAlarmAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public Task SetInputAsync(IoPoint point, bool value, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureInput(point);
        _inputs[point] = value;
        return Task.CompletedTask;
    }

    public bool IsAxisHomed(AxisId axis) => _homedAxes.Contains(axis);

    private void ApplySimulatedFeedback(IoPoint output, bool value)
    {
        // Auto-complete actuator feedback so sequence timeout/alarm logic can be tested without real I/O.
        switch (output)
        {
            case IoPoint.CylinderForward when value:
                _inputs[IoPoint.CylinderFrontSensor] = true;
                _inputs[IoPoint.CylinderRearSensor] = false;
                _outputs[IoPoint.CylinderBackward] = false;
                break;
            case IoPoint.CylinderBackward when value:
                _inputs[IoPoint.CylinderFrontSensor] = false;
                _inputs[IoPoint.CylinderRearSensor] = true;
                _outputs[IoPoint.CylinderForward] = false;
                break;
            case IoPoint.ChamberADoorOpen when value:
                SetDoorFeedback(IoPoint.ChamberADoorOpenSensor, IoPoint.ChamberADoorCloseSensor);
                _outputs[IoPoint.ChamberADoorClose] = false;
                break;
            case IoPoint.ChamberADoorClose when value:
                SetDoorFeedback(IoPoint.ChamberADoorCloseSensor, IoPoint.ChamberADoorOpenSensor);
                _outputs[IoPoint.ChamberADoorOpen] = false;
                break;
            case IoPoint.ChamberBDoorOpen when value:
                SetDoorFeedback(IoPoint.ChamberBDoorOpenSensor, IoPoint.ChamberBDoorCloseSensor);
                _outputs[IoPoint.ChamberBDoorClose] = false;
                break;
            case IoPoint.ChamberBDoorClose when value:
                SetDoorFeedback(IoPoint.ChamberBDoorCloseSensor, IoPoint.ChamberBDoorOpenSensor);
                _outputs[IoPoint.ChamberBDoorOpen] = false;
                break;
            case IoPoint.ChamberCDoorOpen when value:
                SetDoorFeedback(IoPoint.ChamberCDoorOpenSensor, IoPoint.ChamberCDoorCloseSensor);
                _outputs[IoPoint.ChamberCDoorClose] = false;
                break;
            case IoPoint.ChamberCDoorClose when value:
                SetDoorFeedback(IoPoint.ChamberCDoorCloseSensor, IoPoint.ChamberCDoorOpenSensor);
                _outputs[IoPoint.ChamberCDoorOpen] = false;
                break;
            case IoPoint.VacuumSuction when value:
                _outputs[IoPoint.VacuumExhaust] = false;
                break;
            case IoPoint.VacuumExhaust when value:
                _outputs[IoPoint.VacuumSuction] = false;
                break;
        }
    }

    private void SetDoorFeedback(IoPoint activeSensor, IoPoint inactiveSensor)
    {
        _inputs[activeSensor] = true;
        _inputs[inactiveSensor] = false;
    }

    private void TurnOffAllOutputs()
    {
        foreach (var point in _outputs.Keys.ToArray())
        {
            _outputs[point] = false;
        }
    }

    private void EnsureConnected()
    {
        if (!IsConnected)
        {
            throw new InvalidOperationException("The simulated EtherCAT controller is not connected.");
        }
    }

    private void EnsureServoOn()
    {
        if (!_servoOn)
        {
            throw new InvalidOperationException("Servo is OFF.");
        }
    }

    private void EnsureInput(IoPoint point)
    {
        if (!_inputs.ContainsKey(point))
        {
            throw new ArgumentException($"{point} is not configured as a digital input.", nameof(point));
        }
    }

    private void EnsureOutput(IoPoint point)
    {
        if (!_outputs.ContainsKey(point))
        {
            throw new ArgumentException($"{point} is not configured as a digital output.", nameof(point));
        }
    }
}
