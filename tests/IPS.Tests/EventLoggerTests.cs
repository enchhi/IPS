using Xunit;

namespace IPS.Tests;

public class EventLoggerTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"events-{Guid.NewGuid():N}.log");

    public void Dispose()
    {
        if (File.Exists(_path)) File.Delete(_path);
    }

    [Fact]
    public async Task Writes_Line_To_File()
    {
        var id = Guid.NewGuid();
        using (var logger = new EventLogger(_path))
        {
            await logger.WriteAsync("COMPLETED", id, 42);
        }

        var lines = File.ReadAllLines(_path);
        Assert.Single(lines);
        Assert.Contains("COMPLETED", lines[0]);
        Assert.Contains(id.ToString(), lines[0]);
    }

    [Fact]
    public async Task Format_Matches_Spec()
    {
        var id = Guid.NewGuid();
        using (var logger = new EventLogger(_path))
        {
            await logger.WriteAsync("COMPLETED", id, 7);
        }

        string line = File.ReadAllLines(_path)[0];
        // [DateTime] [Status] JobId, Result
        Assert.Matches(@"^\[\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3}\] \[COMPLETED\] [0-9a-f-]+, 7$", line);
    }
}
