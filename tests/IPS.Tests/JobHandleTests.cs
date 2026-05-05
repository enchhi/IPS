using Xunit;

namespace IPS.Tests;

public class JobHandleTests
{
    [Fact]
    public async Task Complete_Sets_Result()
    {
        var handle = new JobHandle(Guid.NewGuid());
        handle.TryComplete(42);
        Assert.Equal(42, await handle.Result);
    }

    [Fact]
    public async Task Abort_Throws_JobAborted()
    {
        var handle = new JobHandle(Guid.NewGuid());
        handle.TryAbort(new InvalidOperationException("boom"));
        await Assert.ThrowsAsync<JobAbortedException>(async () => await handle.Result);
    }
}
