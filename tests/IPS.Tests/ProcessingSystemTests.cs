using Xunit;

namespace IPS.Tests;

public class ProcessingSystemTests
{
    private static SystemConfig Config(int workers = 2, int maxQueue = 100, List<Job>? initial = null) =>
        new()
        {
            WorkerCount = workers,
            MaxQueueSize = maxQueue,
            InitialJobs = initial ?? new List<Job>(),
        };

    private static string TempDir() =>
        Path.Combine(Path.GetTempPath(), $"ips-{Guid.NewGuid():N}");

    [Fact]
    public async Task Submit_Prime_Returns_Correct_Count()
    {
        using var system = new ProcessingSystem(Config(), TempDir());
        var handle = system.Submit(new Job(JobType.Prime, "numbers:100,threads:2", 1));

        int result = await handle.Result.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(25, result);
    }

    [Fact]
    public async Task Submit_IO_Returns_Number_In_Range()
    {
        using var system = new ProcessingSystem(Config(), TempDir());
        var handle = system.Submit(new Job(JobType.IO, "delay:30", 1));

        int result = await handle.Result.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.InRange(result, 0, 100);
    }

    [Fact]
    public void Submit_Same_Id_Twice_Returns_Same_Handle()
    {
        using var system = new ProcessingSystem(Config(), TempDir());

        var id = Guid.NewGuid();
        var first = system.Submit(new Job(id, JobType.IO, "delay:50", 1));
        var second = system.Submit(new Job(id, JobType.IO, "delay:50", 1));

        Assert.Same(first, second);
    }

    [Fact]
    public void Submit_Throws_When_Queue_Full()
    {
        using var system = new ProcessingSystem(Config(workers: 0, maxQueue: 2), TempDir());

        system.Submit(new Job(JobType.IO, "delay:50", 1));
        system.Submit(new Job(JobType.IO, "delay:50", 1));

        Assert.Throws<InvalidOperationException>(() =>
            system.Submit(new Job(JobType.IO, "delay:50", 1)));
    }

    [Fact]
    public void GetJob_Throws_When_Not_Found()
    {
        using var system = new ProcessingSystem(Config(), TempDir());
        Assert.Throws<KeyNotFoundException>(() => system.GetJob(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetTopJobs_Sorted_By_Priority()
    {
        using var system = new ProcessingSystem(Config(workers: 1), TempDir())
        {
            AttemptTimeout = TimeSpan.FromSeconds(30),
        };

        system.Submit(new Job(JobType.IO, "delay:10000", 1));
        system.Submit(new Job(JobType.IO, "delay:10000", 3));
        system.Submit(new Job(JobType.IO, "delay:10000", 1));
        system.Submit(new Job(JobType.IO, "delay:10000", 2));

        await Task.Delay(50);

        var top = system.GetTopJobs(5).ToArray();
        var priorities = top.Select(j => j.Priority).ToArray();
        Assert.Equal(priorities.OrderBy(p => p), priorities);
    }

    [Fact]
    public async Task GetJob_Finds_Submitted_Job()
    {
        using var system = new ProcessingSystem(Config(), TempDir());
        var job = new Job(JobType.IO, "delay:20", 1);
        system.Submit(job);
        await Task.Delay(50);

        var found = system.GetJob(job.Id);
        Assert.NotNull(found);
    }

    [Fact]
    public async Task Initial_Jobs_From_Config_Are_Processed()
    {
        var config = Config(initial: new List<Job>
        {
            new Job(JobType.Prime, "numbers:50,threads:1", 1),
            new Job(JobType.IO, "delay:20", 2),
        });

        using var system = new ProcessingSystem(config, TempDir());
        await Task.Delay(500);

        Assert.Equal(2, system.Executions.Count);
    }

    [Fact]
    public async Task Long_Running_IO_Times_Out_And_Aborts()
    {
        using var system = new ProcessingSystem(Config(workers: 1), TempDir())
        {
            AttemptTimeout = TimeSpan.FromMilliseconds(150),
        };

        var handle = system.Submit(new Job(JobType.IO, "delay:5000", 1));

        await Assert.ThrowsAsync<JobAbortedException>(
            async () => await handle.Result.WaitAsync(TimeSpan.FromSeconds(5)));
    }

    [Fact]
    public async Task JobCompleted_Event_Fires()
    {
        using var system = new ProcessingSystem(Config(), TempDir());
        var captured = new TaskCompletionSource<int>();
        system.JobCompleted += (_, e) => captured.TrySetResult(e.Result);

        system.Submit(new Job(JobType.IO, "delay:20", 1));

        int value = await captured.Task.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.InRange(value, 0, 100);
    }

    [Fact]
    public async Task GenerateReport_Produces_File()
    {
        using var system = new ProcessingSystem(Config(), TempDir());
        var handle = system.Submit(new Job(JobType.IO, "delay:20", 1));
        await handle.Result.WaitAsync(TimeSpan.FromSeconds(3));

        string path = system.GenerateReport();
        Assert.True(File.Exists(path));
    }
}
