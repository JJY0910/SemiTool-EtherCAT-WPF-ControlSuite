namespace SemiTool.Domain;

public sealed class EquipmentStatus
{
    public OperatingMode Mode { get; set; } = OperatingMode.Simulator;
    public MachineState MachineState { get; set; } = MachineState.Offline;
    public bool IsConnected { get; set; }
    public bool IsHardwareUnlocked { get; set; }
    public bool IsAutoRunning { get; set; }
    public bool IsPaused { get; set; }
    public bool IsHomedZ { get; set; }
    public bool IsHomedTheta { get; set; }
    public string CurrentStep { get; set; } = "Idle";
    public int StepNumber { get; set; }
    public string SelectedRecipe { get; set; } = "PreClean_Default";
    public string WaferTransferSummary { get; set; } = "No transfer in progress";
    public string AlarmSummary { get; set; } = "No active alarms";
    public long ZPosition { get; set; }
    public long ThetaPosition { get; set; }
    public Dictionary<IoPoint, bool> Inputs { get; set; } = new();
    public Dictionary<IoPoint, bool> Outputs { get; set; } = new();
}

public sealed class AlarmRecord
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public AlarmCode Code { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Cause { get; init; } = string.Empty;
    public string RecoveryAction { get; init; } = string.Empty;
    public DateTimeOffset OccurredTime { get; init; } = DateTimeOffset.Now;
    public DateTimeOffset? ClearedTime { get; set; }
    public bool IsActive => ClearedTime is null;
}

public sealed record EventLogEntry(
    DateTimeOffset Timestamp,
    string Level,
    string Source,
    string Message);
