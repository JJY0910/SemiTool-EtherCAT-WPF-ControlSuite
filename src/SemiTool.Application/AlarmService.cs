using SemiTool.Domain;

namespace SemiTool.Application;

public sealed class AlarmService
{
    private readonly List<AlarmRecord> _alarms = new();
    private readonly object _gate = new();

    public event EventHandler? AlarmsChanged;

    public IReadOnlyList<AlarmRecord> Alarms
    {
        get
        {
            lock (_gate)
            {
                return _alarms.ToArray();
            }
        }
    }

    public IReadOnlyList<AlarmRecord> ActiveAlarms
    {
        get
        {
            lock (_gate)
            {
                return _alarms.Where(alarm => alarm.IsActive).ToArray();
            }
        }
    }

    public AlarmRecord Raise(AlarmCode code, string name, string cause, string recoveryAction)
    {
        var alarm = new AlarmRecord
        {
            Code = code,
            Name = name,
            Cause = cause,
            RecoveryAction = recoveryAction,
            OccurredTime = DateTimeOffset.Now
        };

        lock (_gate)
        {
            _alarms.Add(alarm);
        }

        AlarmsChanged?.Invoke(this, EventArgs.Empty);
        return alarm;
    }

    public void ClearAll()
    {
        lock (_gate)
        {
            foreach (var alarm in _alarms.Where(alarm => alarm.IsActive))
            {
                alarm.ClearedTime = DateTimeOffset.Now;
            }
        }

        AlarmsChanged?.Invoke(this, EventArgs.Empty);
    }
}
