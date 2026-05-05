using Xunit;

namespace IPS.Tests;

public class SystemConfigTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"cfg-{Guid.NewGuid():N}.xml");

    public void Dispose()
    {
        if (File.Exists(_path)) File.Delete(_path);
    }

    [Fact]
    public void Load_Reads_Worker_Count_And_Queue_Size()
    {
        File.WriteAllText(_path, """
            <SystemConfig>
                <WorkerCount>5</WorkerCount>
                <MaxQueueSize>100</MaxQueueSize>
            </SystemConfig>
            """);

        var config = SystemConfig.Load(_path);

        Assert.Equal(5, config.WorkerCount);
        Assert.Equal(100, config.MaxQueueSize);
    }

    [Fact]
    public void Load_Reads_Initial_Jobs()
    {
        File.WriteAllText(_path, """
            <SystemConfig>
                <WorkerCount>2</WorkerCount>
                <MaxQueueSize>10</MaxQueueSize>
                <Jobs>
                    <Job Type="Prime" Payload="numbers:100,threads:2" Priority="1"/>
                    <Job Type="IO" Payload="delay:500" Priority="3"/>
                </Jobs>
            </SystemConfig>
            """);

        var config = SystemConfig.Load(_path);

        Assert.Equal(2, config.InitialJobs.Count);
    }

    [Fact]
    public void Load_Throws_When_File_Missing()
    {
        Assert.Throws<FileNotFoundException>(
            () => SystemConfig.Load(Path.Combine(Path.GetTempPath(), "nope.xml")));
    }
}
