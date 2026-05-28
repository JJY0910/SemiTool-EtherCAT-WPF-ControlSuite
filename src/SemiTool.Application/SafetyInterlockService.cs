using SemiTool.Domain;
using SemiTool.Hardware;

namespace SemiTool.Application;

public sealed class SafetyInterlockService
{
    private readonly AlarmService _alarms;
    private readonly EventLogService _events;

    public SafetyInterlockService(AlarmService alarms, EventLogService events)
    {
        _alarms = alarms;
        _events = events;
    }

    public bool IsAutoRunning { get; private set; }
    public bool IsPaused { get; private set; }
    public bool IsHomedZ { get; private set; }
    public bool IsHomedTheta { get; private set; }
    public MachineState MachineState { get; private set; } = MachineState.Offline;

    public void MarkConnected() => MachineState = MachineState.Idle;

    public void MarkDisconnected()
    {
        IsAutoRunning = false;
        IsPaused = false;
        MachineState = MachineState.Offline;
    }

    public void MarkHomed(AxisId axis)
    {
        if (axis == AxisId.Z)
        {
            IsHomedZ = true;
        }
        else
        {
            IsHomedTheta = true;
        }
    }

    public void EnsureManualAllowed()
    {
        if (!IsAutoRunning)
        {
            return;
        }

        _alarms.Raise(
            AlarmCode.ManualBlockedDuringAuto,
            "Manual command blocked",
            "A manual command was requested while Auto was running.",
            "Stop or pause Auto before using Manual Control.");
        throw new InvalidOperationException("Manual commands are blocked while Auto is running.");
    }

    public void BeginAuto(IEthercatController controller)
    {
        if (!controller.IsConnected)
        {
            _alarms.Raise(
                AlarmCode.NotConnected,
                "Auto start blocked",
                "Auto Start was requested while the EtherCAT controller was disconnected.",
                "Connect the selected controller and verify status.");
            throw new InvalidOperationException("Auto start requires a connected controller.");
        }

        if (!IsHomedZ || !IsHomedTheta)
        {
            _alarms.Raise(
                AlarmCode.HomingRequired,
                "Homing required",
                "Auto Start was requested before Z and Theta homing were complete.",
                "Servo ON, home Z, and home Theta before Auto Start.");
            throw new InvalidOperationException("Auto start requires Z and Theta homing.");
        }

        IsAutoRunning = true;
        IsPaused = false;
        MachineState = MachineState.AutoRunning;
        _events.Info(nameof(SafetyInterlockService), "Auto mode started.");
    }

    public void StopAuto()
    {
        IsAutoRunning = false;
        IsPaused = false;
        MachineState = MachineState.Idle;
        _events.Info(nameof(SafetyInterlockService), "Auto mode stopped.");
    }

    public void PauseAuto()
    {
        if (!IsAutoRunning)
        {
            return;
        }

        IsPaused = true;
        MachineState = MachineState.Paused;
        _events.Info(nameof(SafetyInterlockService), "Auto mode paused.");
    }

    public void ResumeAuto()
    {
        if (!IsAutoRunning)
        {
            return;
        }

        IsPaused = false;
        MachineState = MachineState.AutoRunning;
        _events.Info(nameof(SafetyInterlockService), "Auto mode resumed.");
    }

    public void SetAlarmState()
    {
        IsAutoRunning = false;
        IsPaused = false;
        MachineState = MachineState.Alarm;
    }

    public void SetEmergencyState()
    {
        IsAutoRunning = false;
        IsPaused = false;
        MachineState = MachineState.Emergency;
    }

    public void Reset()
    {
        IsAutoRunning = false;
        IsPaused = false;
        MachineState = MachineState.Idle;
        _alarms.ClearAll();
        _events.Info(nameof(SafetyInterlockService), "Safety state reset.");
    }
}
