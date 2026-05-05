using Xunit;

namespace IPS.Tests;

public class JobTests
{
    [Fact]
    public void Default_Constructor_Generates_Id()
    {
        var job = new Job();
        Assert.NotEqual(Guid.Empty, job.Id);
    }

    [Fact]
    public void Constructor_Sets_Fields()
    {
        var job = new Job(JobType.IO, "delay:500", 3);
        Assert.Equal(JobType.IO, job.Type);
        Assert.Equal("delay:500", job.Payload);
        Assert.Equal(3, job.Priority);
    }
}
