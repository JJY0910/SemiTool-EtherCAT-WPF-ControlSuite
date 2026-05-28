using System.Text;
using SemiTool.Domain;

namespace SemiTool.Application;

public sealed class EventLogService
{
    private readonly List<EventLogEntry> _entries = new();
    private readonly object _gate = new();

    public event EventHandler? EntriesChanged;

    public IReadOnlyList<EventLogEntry> Entries
    {
        get
        {
            lock (_gate)
            {
                return _entries.ToArray();
            }
        }
    }

    public void Info(string source, string message) => Add("INFO", source, message);

    public void Warn(string source, string message) => Add("WARN", source, message);

    public void Error(string source, string message) => Add("ERROR", source, message);

    public void Add(string level, string source, string message)
    {
        lock (_gate)
        {
            _entries.Add(new EventLogEntry(DateTimeOffset.Now, level, source, message));
        }

        EntriesChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task ExportCsvAsync(string path, CancellationToken cancellationToken = default)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Timestamp,Level,Source,Message");
        foreach (var entry in Entries)
        {
            builder.Append(Csv(entry.Timestamp.ToString("O")));
            builder.Append(',');
            builder.Append(Csv(entry.Level));
            builder.Append(',');
            builder.Append(Csv(entry.Source));
            builder.Append(',');
            builder.AppendLine(Csv(entry.Message));
        }

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(path, builder.ToString(), cancellationToken).ConfigureAwait(false);
    }

    private static string Csv(string value) => $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
}
