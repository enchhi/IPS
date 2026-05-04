using System.Globalization;
using System.Xml.Linq;

namespace IPS;

public class ReportGenerator
{
    public const int MaxReports = 10;

    private string directory;
    private int counter;

    public ReportGenerator(string directory)
    {
        this.directory = directory;
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);
    }

    public string Generate(IEnumerable<ExecutionRecord> snapshot)
    {
        var data = snapshot.ToArray();

        var executedByType = data
            .Where(r => r.Outcome == JobOutcome.Completed)
            .GroupBy(r => r.Type)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .OrderBy(x => x.Type)
            .ToArray();

        var avgDurationByType = data
            .Where(r => r.Outcome == JobOutcome.Completed)
            .GroupBy(r => r.Type)
            .Select(g => new { Type = g.Key, AverageMs = g.Average(r => r.Duration.TotalMilliseconds) })
            .OrderBy(x => x.Type)
            .ToArray();

        var failedByType = data
            .Where(r => r.Outcome == JobOutcome.Aborted)
            .GroupBy(r => r.Type)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .OrderBy(x => x.Type)
            .ToArray();

        var doc = new XDocument(
            new XElement("Report",
                new XAttribute("GeneratedAt", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")),
                new XElement("ExecutedByType",
                    executedByType.Select(x => new XElement("Item",
                        new XAttribute("Type", x.Type),
                        new XAttribute("Count", x.Count)))),
                new XElement("AverageDurationByType",
                    avgDurationByType.Select(x => new XElement("Item",
                        new XAttribute("Type", x.Type),
                        new XAttribute("AverageMs", x.AverageMs.ToString("F2", CultureInfo.InvariantCulture))))),
                new XElement("FailedByType",
                    failedByType.Select(x => new XElement("Item",
                        new XAttribute("Type", x.Type),
                        new XAttribute("Count", x.Count))))));

        int index = counter++;
        string path = Path.Combine(directory, $"report_{index % MaxReports}.xml");
        doc.Save(path);
        return path;
    }
}
