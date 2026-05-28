using System.Diagnostics;
using SemiTool.Domain;
using SemiTool.Hardware;

namespace SemiTool.Application;

public sealed class EquipmentSequenceService
{
    private readonly IEthercatController _ethercat;
    private readonly EquipmentProfile _profile;
    private readonly SafetyInterlockService _safety;
    private readonly AlarmService _alarms;
    private readonly EventLogService _events;

    public EquipmentSequenceService(
        IEthercatController ethercat,
        EquipmentProfile profile,
        SafetyInterlockService safety,
        AlarmService alarms,
        EventLogService events)
    {
        _ethercat = ethercat;
        _profile = profile;
        _safety = safety;
        _alarms = alarms;
        _events = events;
    }

    public string CurrentSequenceName { get; private set; } = "Idle";
    public int StepNumber { get; private set; }
    public string StepDescription { get; private set; } = "Ready";
    public TimeSpan Elapsed { get; private set; }
    public TimeSpan Timeout { get; private set; }

    public async Task ServoOnAsync(CancellationToken cancellationToken = default)
    {
        _safety.EnsureManualAllowed();
        await _ethercat.ServoOnAsync(cancellationToken).ConfigureAwait(false);
        _events.Info(nameof(EquipmentSequenceService), "Servo ON command completed.");
    }

    public async Task ServoOffAsync(CancellationToken cancellationToken = default)
    {
        _safety.EnsureManualAllowed();
        await _ethercat.ServoOffAsync(cancellationToken).ConfigureAwait(false);
        _events.Info(nameof(EquipmentSequenceService), "Servo OFF command completed.");
    }

    public async Task HomeAxisAsync(AxisId axis, CancellationToken cancellationToken = default)
    {
        _safety.EnsureManualAllowed();
        await _ethercat.HomeAxisAsync(axis, cancellationToken).ConfigureAwait(false);
        _safety.MarkHomed(axis);
        _events.Info(nameof(EquipmentSequenceService), $"{axis} homing completed.");
    }

    public async Task MoveAxisAbsoluteAsync(AxisId axis, long targetPosition, CancellationToken cancellationToken = default)
    {
        _safety.EnsureManualAllowed();
        await _ethercat.MoveAxisAbsoluteAsync(axis, targetPosition, cancellationToken).ConfigureAwait(false);
        _events.Info(nameof(EquipmentSequenceService), $"{axis} axis moved to {targetPosition}.");
    }

    public Task MoveToPose(RobotPose pose, CancellationToken cancellationToken = default) =>
        MoveToPoseAsync(pose, cancellationToken);

    public async Task MoveToPoseAsync(RobotPose pose, CancellationToken cancellationToken = default)
    {
        _safety.EnsureManualAllowed();
        await MoveToPoseCoreAsync(pose, includeWorkPosition: false, cancellationToken).ConfigureAwait(false);
    }

    public Task MoveToNamedPoseAsync(string poseKey, CancellationToken cancellationToken = default) =>
        MoveToPoseAsync(_profile.GetPose(poseKey), cancellationToken);

    public Task PickFromFoupA(int slot, CancellationToken cancellationToken = default) =>
        PickFromFoupAAsync(slot, cancellationToken);

    public async Task PickFromFoupAAsync(int slot, CancellationToken cancellationToken = default)
    {
        await RunSequenceAsync($"Pick FOUP A Slot {slot}", cancellationToken, async ct =>
        {
            var basePose = _profile.GetPose("FoupA");
            var slotPose = _profile.GetFoupSlotPose(slot);
            await MoveToFoupSlotCoreAsync(basePose.Theta, slotPose, ct).ConfigureAwait(false);
            await CylinderForwardCoreAsync(ct).ConfigureAwait(false);
            await VacuumSuctionCoreAsync(ct).ConfigureAwait(false);
            await CylinderBackwardCoreAsync(ct).ConfigureAwait(false);
            await MoveAxisCoreAsync(AxisId.Z, slotPose.ZSafe, ct).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    public Task PlaceToChamber(ChamberId chamber, CancellationToken cancellationToken = default) =>
        PlaceToChamberAsync(chamber, cancellationToken);

    public async Task PlaceToChamberAsync(ChamberId chamber, CancellationToken cancellationToken = default)
    {
        await RunSequenceAsync($"Place to Chamber {chamber}", cancellationToken, async ct =>
        {
            await OpenChamberDoorCoreAsync(chamber, ct).ConfigureAwait(false);
            await MoveToPoseCoreAsync(_profile.GetChamberPose(chamber), includeWorkPosition: true, ct).ConfigureAwait(false);
            await CylinderForwardCoreAsync(ct).ConfigureAwait(false);
            await VacuumExhaustCoreAsync(ct).ConfigureAwait(false);
            await CylinderBackwardCoreAsync(ct).ConfigureAwait(false);
            await CloseChamberDoorCoreAsync(chamber, ct).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    public Task PickFromChamber(ChamberId chamber, CancellationToken cancellationToken = default) =>
        PickFromChamberAsync(chamber, cancellationToken);

    public async Task PickFromChamberAsync(ChamberId chamber, CancellationToken cancellationToken = default)
    {
        await RunSequenceAsync($"Pick from Chamber {chamber}", cancellationToken, async ct =>
        {
            await OpenChamberDoorCoreAsync(chamber, ct).ConfigureAwait(false);
            await MoveToPoseCoreAsync(_profile.GetChamberPose(chamber), includeWorkPosition: true, ct).ConfigureAwait(false);
            await CylinderForwardCoreAsync(ct).ConfigureAwait(false);
            await VacuumSuctionCoreAsync(ct).ConfigureAwait(false);
            await CylinderBackwardCoreAsync(ct).ConfigureAwait(false);
            await CloseChamberDoorCoreAsync(chamber, ct).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    public Task PlaceToFoupB(int slot, CancellationToken cancellationToken = default) =>
        PlaceToFoupBAsync(slot, cancellationToken);

    public async Task PlaceToFoupBAsync(int slot, CancellationToken cancellationToken = default)
    {
        await RunSequenceAsync($"Place FOUP B Slot {slot}", cancellationToken, async ct =>
        {
            var basePose = _profile.GetPose("FoupB");
            var slotPose = _profile.GetFoupSlotPose(slot);
            await MoveToFoupSlotCoreAsync(basePose.Theta, slotPose, ct).ConfigureAwait(false);
            await CylinderForwardCoreAsync(ct).ConfigureAwait(false);
            await VacuumExhaustCoreAsync(ct).ConfigureAwait(false);
            await CylinderBackwardCoreAsync(ct).ConfigureAwait(false);
            await MoveAxisCoreAsync(AxisId.Z, slotPose.ZSafe, ct).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    public Task CylinderForwardAsync(CancellationToken cancellationToken = default)
    {
        _safety.EnsureManualAllowed();
        return CylinderForwardCoreAsync(cancellationToken);
    }

    public Task CylinderBackwardAsync(CancellationToken cancellationToken = default)
    {
        _safety.EnsureManualAllowed();
        return CylinderBackwardCoreAsync(cancellationToken);
    }

    public Task VacuumSuctionAsync(CancellationToken cancellationToken = default)
    {
        _safety.EnsureManualAllowed();
        return VacuumSuctionCoreAsync(cancellationToken);
    }

    public Task VacuumExhaustAsync(CancellationToken cancellationToken = default)
    {
        _safety.EnsureManualAllowed();
        return VacuumExhaustCoreAsync(cancellationToken);
    }

    public Task OpenChamberDoorAsync(ChamberId chamber, CancellationToken cancellationToken = default)
    {
        _safety.EnsureManualAllowed();
        return OpenChamberDoorCoreAsync(chamber, cancellationToken);
    }

    public Task CloseChamberDoorAsync(ChamberId chamber, CancellationToken cancellationToken = default)
    {
        _safety.EnsureManualAllowed();
        return CloseChamberDoorCoreAsync(chamber, cancellationToken);
    }

    public async Task SetOutputAsync(IoPoint point, bool value, CancellationToken cancellationToken = default)
    {
        _safety.EnsureManualAllowed();
        await _ethercat.WriteDigitalOutputAsync(point, value, cancellationToken).ConfigureAwait(false);
        _events.Info(nameof(EquipmentSequenceService), $"{point.GetDisplayName()} set to {value}.");
    }

    public async Task EmergencyStopAsync(CancellationToken cancellationToken = default)
    {
        await _ethercat.EmergencyStopAsync(cancellationToken).ConfigureAwait(false);
        _safety.SetEmergencyState();
        _alarms.Raise(
            AlarmCode.EmergencyStop,
            "Emergency stop",
            "Emergency Stop was requested by the operator or safety logic.",
            "Inspect equipment, clear the hazard, reset alarms, and reconnect if required.");
        _events.Error(nameof(EquipmentSequenceService), "Emergency Stop executed.");
    }

    public async Task ResetAsync(CancellationToken cancellationToken = default)
    {
        await _ethercat.ResetAlarmAsync(cancellationToken).ConfigureAwait(false);
        _safety.Reset();
    }

    private async Task RunSequenceAsync(string name, CancellationToken cancellationToken, Func<CancellationToken, Task> body)
    {
        var stopwatch = Stopwatch.StartNew();
        CurrentSequenceName = name;
        StepNumber = 0;
        StepDescription = "Starting";
        _events.Info(nameof(EquipmentSequenceService), $"{name} started.");

        try
        {
            await body(cancellationToken).ConfigureAwait(false);
            StepDescription = "Completed";
            _events.Info(nameof(EquipmentSequenceService), $"{name} completed.");
        }
        catch (OperationCanceledException)
        {
            StepDescription = "Canceled";
            _events.Warn(nameof(EquipmentSequenceService), $"{name} canceled.");
            throw;
        }
        catch (Exception ex)
        {
            _safety.SetAlarmState();
            _alarms.Raise(
                AlarmCode.SequenceFailed,
                "Sequence failed",
                ex.Message,
                "Review the event log, recover hardware, reset alarms, and retry.");
            _events.Error(nameof(EquipmentSequenceService), $"{name} failed: {ex.Message}");
            throw;
        }
        finally
        {
            stopwatch.Stop();
            Elapsed = stopwatch.Elapsed;
        }
    }

    private async Task MoveToFoupSlotCoreAsync(long theta, FoupSlotPose slotPose, CancellationToken cancellationToken)
    {
        await MoveAxisCoreAsync(AxisId.Z, slotPose.ZSafe, cancellationToken).ConfigureAwait(false);
        await MoveAxisCoreAsync(AxisId.Theta, theta, cancellationToken).ConfigureAwait(false);
        await MoveAxisCoreAsync(AxisId.Z, slotPose.ZWork, cancellationToken).ConfigureAwait(false);
    }

    private async Task MoveToPoseCoreAsync(RobotPose pose, bool includeWorkPosition, CancellationToken cancellationToken)
    {
        await MoveAxisCoreAsync(AxisId.Z, pose.ZSafe, cancellationToken).ConfigureAwait(false);
        await MoveAxisCoreAsync(AxisId.Theta, pose.Theta, cancellationToken).ConfigureAwait(false);
        if (includeWorkPosition)
        {
            await MoveAxisCoreAsync(AxisId.Z, pose.ZWork, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task MoveAxisCoreAsync(AxisId axis, long target, CancellationToken cancellationToken)
    {
        StepNumber++;
        StepDescription = $"Move {axis} to {target}";
        await _ethercat.MoveAxisAbsoluteAsync(axis, target, cancellationToken).ConfigureAwait(false);
        await DelayAsync(_profile.Timing.MotionWaitMs, cancellationToken).ConfigureAwait(false);
    }

    private async Task CylinderForwardCoreAsync(CancellationToken cancellationToken)
    {
        StepNumber++;
        StepDescription = "Cylinder forward";
        await _ethercat.WriteDigitalOutputAsync(IoPoint.CylinderBackward, false, cancellationToken).ConfigureAwait(false);
        await _ethercat.WriteDigitalOutputAsync(IoPoint.CylinderForward, true, cancellationToken).ConfigureAwait(false);
        await WaitForInputStateAsync(
            IoPoint.CylinderFrontSensor,
            true,
            _profile.Timing.CylinderWaitTimeoutMs,
            AlarmCode.CylinderTimeout,
            "Cylinder forward timeout",
            "DI13 did not indicate Cylinder Front Sensor before timeout.",
            cancellationToken).ConfigureAwait(false);
    }

    private async Task CylinderBackwardCoreAsync(CancellationToken cancellationToken)
    {
        StepNumber++;
        StepDescription = "Cylinder backward";
        await _ethercat.WriteDigitalOutputAsync(IoPoint.CylinderForward, false, cancellationToken).ConfigureAwait(false);
        await _ethercat.WriteDigitalOutputAsync(IoPoint.CylinderBackward, true, cancellationToken).ConfigureAwait(false);
        await WaitForInputStateAsync(
            IoPoint.CylinderRearSensor,
            true,
            _profile.Timing.CylinderWaitTimeoutMs,
            AlarmCode.CylinderTimeout,
            "Cylinder backward timeout",
            "DI12 did not indicate Cylinder Rear Sensor before timeout.",
            cancellationToken).ConfigureAwait(false);
    }

    private async Task VacuumSuctionCoreAsync(CancellationToken cancellationToken)
    {
        StepNumber++;
        StepDescription = "Vacuum suction";
        await _ethercat.WriteDigitalOutputAsync(IoPoint.VacuumExhaust, false, cancellationToken).ConfigureAwait(false);
        await _ethercat.WriteDigitalOutputAsync(IoPoint.VacuumSuction, true, cancellationToken).ConfigureAwait(false);
        await DelayAsync(_profile.Timing.VacuumSuctionMs, cancellationToken).ConfigureAwait(false);
    }

    private async Task VacuumExhaustCoreAsync(CancellationToken cancellationToken)
    {
        StepNumber++;
        StepDescription = "Vacuum exhaust";
        await _ethercat.WriteDigitalOutputAsync(IoPoint.VacuumSuction, false, cancellationToken).ConfigureAwait(false);
        await _ethercat.WriteDigitalOutputAsync(IoPoint.VacuumExhaust, true, cancellationToken).ConfigureAwait(false);
        await DelayAsync(_profile.Timing.VacuumExhaustMs, cancellationToken).ConfigureAwait(false);
        await _ethercat.WriteDigitalOutputAsync(IoPoint.VacuumExhaust, false, cancellationToken).ConfigureAwait(false);
    }

    private async Task OpenChamberDoorCoreAsync(ChamberId chamber, CancellationToken cancellationToken)
    {
        var map = DoorMap(chamber);
        StepNumber++;
        StepDescription = $"Open chamber {chamber} door";
        await _ethercat.WriteDigitalOutputAsync(map.CloseOutput, false, cancellationToken).ConfigureAwait(false);
        await _ethercat.WriteDigitalOutputAsync(map.OpenOutput, true, cancellationToken).ConfigureAwait(false);
        await WaitForInputStateAsync(
            map.OpenSensor,
            true,
            _profile.Timing.DoorWaitMs,
            AlarmCode.DoorSensorMismatch,
            $"Chamber {chamber} door open timeout",
            $"{map.OpenSensor.GetDisplayName()} did not turn ON before timeout.",
            cancellationToken).ConfigureAwait(false);
    }

    private async Task CloseChamberDoorCoreAsync(ChamberId chamber, CancellationToken cancellationToken)
    {
        var map = DoorMap(chamber);
        StepNumber++;
        StepDescription = $"Close chamber {chamber} door";
        await _ethercat.WriteDigitalOutputAsync(map.OpenOutput, false, cancellationToken).ConfigureAwait(false);
        await _ethercat.WriteDigitalOutputAsync(map.CloseOutput, true, cancellationToken).ConfigureAwait(false);
        await WaitForInputStateAsync(
            map.CloseSensor,
            true,
            _profile.Timing.DoorWaitMs,
            AlarmCode.DoorSensorMismatch,
            $"Chamber {chamber} door close timeout",
            $"{map.CloseSensor.GetDisplayName()} did not turn ON before timeout.",
            cancellationToken).ConfigureAwait(false);
    }

    private async Task WaitForInputStateAsync(
        IoPoint point,
        bool expected,
        int timeoutMs,
        AlarmCode alarmCode,
        string alarmName,
        string cause,
        CancellationToken cancellationToken)
    {
        Timeout = TimeSpan.FromMilliseconds(timeoutMs);
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.ElapsedMilliseconds <= timeoutMs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var actual = await _ethercat.ReadDigitalInputAsync(point, cancellationToken).ConfigureAwait(false);
            if (actual == expected)
            {
                return;
            }

            await Task.Delay(50, cancellationToken).ConfigureAwait(false);
        }

        _safety.SetAlarmState();
        _alarms.Raise(alarmCode, alarmName, cause, "Check sensor wiring/status, recover actuator, then Reset.");
        throw new TimeoutException(alarmName);
    }

    private static DoorIoMap DoorMap(ChamberId chamber) => chamber switch
    {
        ChamberId.A => new DoorIoMap(
            IoPoint.ChamberADoorOpen,
            IoPoint.ChamberADoorClose,
            IoPoint.ChamberADoorOpenSensor,
            IoPoint.ChamberADoorCloseSensor),
        ChamberId.B => new DoorIoMap(
            IoPoint.ChamberBDoorOpen,
            IoPoint.ChamberBDoorClose,
            IoPoint.ChamberBDoorOpenSensor,
            IoPoint.ChamberBDoorCloseSensor),
        ChamberId.C => new DoorIoMap(
            IoPoint.ChamberCDoorOpen,
            IoPoint.ChamberCDoorClose,
            IoPoint.ChamberCDoorOpenSensor,
            IoPoint.ChamberCDoorCloseSensor),
        _ => throw new ArgumentOutOfRangeException(nameof(chamber), chamber, null)
    };

    private static Task DelayAsync(int milliseconds, CancellationToken cancellationToken) =>
        milliseconds <= 0 ? Task.CompletedTask : Task.Delay(milliseconds, cancellationToken);

    private sealed record DoorIoMap(IoPoint OpenOutput, IoPoint CloseOutput, IoPoint OpenSensor, IoPoint CloseSensor);
}
