using System.Xml.Linq;
using Xunit;

namespace IPS.Tests;

public class ReportGeneratorTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"reports-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_dir)) Directory.Delete(_dir, recursive: true);
    }

    private static IEnumerable<ExecutionRecord> Sample() => new[]
    {
        new ExecutionRecord(Guid.NewGuid(), JobType.Prime, JobOutcome.Completed, TimeSpan.FromMilliseconds(100), DateTime.Now),
        new ExecutionRecord(Guid.NewGuid(), JobType.Prime, JobOutcome.Completed, TimeSpan.FromMilliseconds(200), DateTime.Now),
        new ExecutionRecord(Guid.NewGuid(), JobType.IO,    JobOutcome.Completed, TimeSpan.FromMilliseconds(50),  DateTime.Now),
        new ExecutionRecord(Guid.NewGuid(), JobType.IO,    JobOutcome.Aborted,   TimeSpan.FromMilliseconds(2000), DateTime.Now),
    };

    [Fact]
    public void Generate_Writes_Xml_File()
    {
        var sut = new ReportGenerator(_dir);
        string path = sut.Generate(Sample());
        Assert.True(File.Exists(path));
    }

    [Fact]
    public void Generate_Counts_By_Type()
    {
        var sut = new ReportGenerator(_dir);
        string path = sut.Generate(Sample());
        var doc = XDocument.Load(path);

        var primeCount = doc.Root!.Element("ExecutedByType")!
            .Elements("Item")
            .First(x => (string)x.Attribute("Type")! == "Prime")
            .Attribute("Count")!.Value;

        Assert.Equal("2", primeCount);
    }

    [Fact]
    public void Generate_Computes_Average_By_Type()
    {
        var sut = new ReportGenerator(_dir);
        string path = sut.Generate(Sample());
        var doc = XDocument.Load(path);

        var primeAvg = doc.Root!.Element("AverageDurationByType")!
            .Elements("Item")
            .First(x => (string)x.Attribute("Type")! == "Prime")
            .Attribute("AverageMs")!.Value;

        Assert.Equal("150.00", primeAvg);
    }

    [Fact]
    public void Rotates_After_10_Reports()
    {
        var sut = new ReportGenerator(_dir);
        for (int i = 0; i < ReportGenerator.MaxReports + 3; i++)
            sut.Generate(Sample());

        var files = Directory.GetFiles(_dir, "report_*.xml");
        Assert.Equal(ReportGenerator.MaxReports, files.Length);
    }
}
