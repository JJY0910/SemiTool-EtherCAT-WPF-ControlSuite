using System.Text;
using SemiTool.Domain;

namespace SemiTool.Infrastructure;

public sealed class CsvLogWriter
{
    public async Task WriteEventsAsync(string path, IEnumerable<EventLogEntry> events, CancellationToken cancellationToken = default)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Timestamp,Level,Source,Message");
        foreach (var entry in events)
        {
            builder.AppendLine(string.Join(
                ",",
                Csv(entry.Timestamp.ToString("O")),
                Csv(entry.Level),
                Csv(entry.Source),
                Csv(entry.Message)));
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
