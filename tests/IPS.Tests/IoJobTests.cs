using System.Diagnostics;
using IPS.Processing;
using Xunit;

namespace IPS.Tests;

public class IoJobTests
{
    [Fact]
    public void Result_In_0_To_100()
    {
        int value = IoJob.Execute("delay:0", CancellationToken.None);
        Assert.InRange(value, 0, 100);
    }

    [Fact]
    public void Sleeps_Roughly_For_Configured_Delay()
    {
        var sw = Stopwatch.StartNew();
        IoJob.Execute("delay:200", CancellationToken.None);
        sw.Stop();
        Assert.True(sw.ElapsedMilliseconds >= 150);
    }

    [Fact]
    public void Parse_Handles_Underscores()
    {
        Assert.Equal(1000, IoJob.ParseDelay("delay:1_000"));
    }
}
