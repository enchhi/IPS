using IPS.Processing;
using Xunit;

namespace IPS.Tests;

public class PrimeJobTests
{
    [Fact]
    public void Counts_Primes_Up_To_10()
    {
        // 2, 3, 5, 7
        int count = PrimeJob.Execute("numbers:10,threads:1", CancellationToken.None);
        Assert.Equal(4, count);
    }

    [Fact]
    public void Counts_Primes_Up_To_100()
    {
        int count = PrimeJob.Execute("numbers:100,threads:2", CancellationToken.None);
        Assert.Equal(25, count);
    }

    [Fact]
    public void Returns_Zero_When_Limit_Below_Two()
    {
        Assert.Equal(0, PrimeJob.Execute("numbers:1,threads:1", CancellationToken.None));
    }

    [Fact]
    public void Parse_Clamps_Threads_To_Max_8()
    {
        var (_, threads) = PrimeJob.Parse("numbers:100,threads:50");
        Assert.Equal(8, threads);
    }
}
