using IPS;

namespace IPS.App;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine("=== Industrial Processing System ===");

        SystemConfig config;
        try
        {
            string path = args.Length > 0 ? args[0] : "SystemConfig.xml";
            config = SystemConfig.Load(path);
            Console.WriteLine($"Config: WorkerCount={config.WorkerCount}, MaxQueueSize={config.MaxQueueSize}, InitialJobs={config.InitialJobs.Count}");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to load config: " + ex.Message);
            return 1;
        }

        // logger u koji subscriber-i pisu asinhrono
        var logPath = Path.Combine(AppContext.BaseDirectory, "logs", "events.log");
        using var logger = new EventLogger(logPath);

        using var system = new ProcessingSystem(config);

        // pretplate na dogadjaje (lambda)
        system.JobCompleted += (s, e) =>
        {
            Console.WriteLine($"[OK] {e.Job.Type} {e.Job.Id} -> {e.Result} ({e.Elapsed.TotalMilliseconds:F0} ms)");
            _ = logger.WriteAsync("COMPLETED", e.Job.Id, e.Result);
        };

        system.JobFailed += (s, e) =>
        {
            Console.WriteLine($"[FAIL] {e.Job.Type} {e.Job.Id} attempt={e.Attempt} reason={e.Reason}");
            string status = e.Attempt >= system.MaxAttempts ? "ABORT" : "FAILED";
            string detail = status == "ABORT"
                ? $"after {system.MaxAttempts} attempts"
                : $"attempt={e.Attempt} reason={e.Reason}";
            _ = logger.WriteAsync(status, e.Job.Id, detail);
        };

        // producer niti — broj iz config-a
        using var producerCts = new CancellationTokenSource();
        var producers = new Task[config.WorkerCount];
        for (int i = 0; i < config.WorkerCount; i++)
        {
            int id = i;
            producers[i] = Task.Run(() => ProducerLoop(id, system, producerCts.Token));
        }

        Console.WriteLine($"Started {config.WorkerCount} producer threads. Press ENTER to stop.");
        Console.ReadLine();

        producerCts.Cancel();
        try { await Task.WhenAll(producers); } catch { }

        try
        {
            string reportPath = system.GenerateReport();
            Console.WriteLine("Final report: " + reportPath);
            Console.WriteLine("Event log: " + logPath);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Report failed: " + ex.Message);
        }

        Console.WriteLine("Shutting down...");
        return 0;
    }

    private static async Task ProducerLoop(int id, ProcessingSystem system, CancellationToken ct)
    {
        var rng = new Random(Environment.TickCount + id * 31);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var job = MakeRandomJob(rng);
                system.Submit(job);
            }
            catch (InvalidOperationException)
            {
                Console.WriteLine($"[producer {id}] queue full - rejected");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[producer {id}] {ex.GetType().Name}: {ex.Message}");
            }

            try { await Task.Delay(rng.Next(200, 800), ct); }
            catch (OperationCanceledException) { return; }
        }
    }

    private static Job MakeRandomJob(Random rng)
    {
        if (rng.Next(2) == 0)
        {
            int n = rng.Next(1000, 50000);
            int t = rng.Next(1, 10);
            int prio = rng.Next(1, 5);
            return new Job(JobType.Prime, $"numbers:{n},threads:{t}", prio);
        }
        else
        {
            int delay = rng.Next(10) < 8 ? rng.Next(100, 1500) : rng.Next(2500, 5000);
            int prio = rng.Next(1, 5);
            return new Job(JobType.IO, $"delay:{delay}", prio);
        }
    }
}
