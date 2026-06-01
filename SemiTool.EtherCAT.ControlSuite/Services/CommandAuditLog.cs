using System.Collections.ObjectModel;
using SemiTool.EtherCAT.ControlSuite.Models;

namespace SemiTool.EtherCAT.ControlSuite.Services;

public sealed class CommandAuditLog
{
    private readonly ObservableCollection<CommandAuditRecord> _records = new();

    public ReadOnlyObservableCollection<CommandAuditRecord> Records { get; }

    public CommandAuditLog()
    {
        Records = new ReadOnlyObservableCollection<CommandAuditRecord>(_records);
    }

    public void Append(CommandDecision decision)
    {
        _records.Insert(0, new CommandAuditRecord(
            DateTimeOffset.Now,
            decision.Command.Type,
            decision.Command.Route,
            decision.IsAllowed,
            decision.Reason,
            decision.Command.RequestedBy));

        while (_records.Count > 200)
        {
            _records.RemoveAt(_records.Count - 1);
        }
    }
}
