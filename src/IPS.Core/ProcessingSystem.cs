using System.Diagnostics;
using IPS.Processing;

namespace IPS;

public class ProcessingSystem : IDisposable
{
    public TimeSpan AttemptTimeout { get; set; } = TimeSpan.FromSeconds(2);
    public int MaxAttempts { get; set; } = 3;

    private int maxQueueSize;

    // jednostavan PriorityQueue gde je manji broj veci prioritet
    private PriorityQueue<Job, int> queue = new PriorityQueue<Job, int>();
    private object queueGate = new object();
    private int pending;

    private SemaphoreSlim signal = new SemaphoreSlim(0);

    private Dictionary<Guid, JobHandle> handles = new Dictionary<Guid, JobHandle>();
    private Dictionary<Guid, Job> jobsById = new Dictionary<Guid, Job>();

    private List<ExecutionRecord> executions = new List<ExecutionRecord>();
    public IReadOnlyCollection<ExecutionRecord> Executions
    {
        get { lock (executions) return executions.ToArray(); }
    }

    public event EventHandler<JobCompletedEventArgs>? JobCompleted;
    public event EventHandler<JobFailedEventArgs>? JobFailed;

    private ReportGenerator reports;
    private Timer reportTimer;

    private CancellationTokenSource shutdown = new CancellationTokenSource();
    private Task[] workers;
    private bool disposed;

    public ProcessingSystem(SystemConfig config, string? outputDirectory = null)
    {
        maxQueueSize = config.MaxQueueSize;

        var baseDir = outputDirectory ?? AppContext.BaseDirectory;
        reports = new ReportGenerator(Path.Combine(baseDir, "reports"));

        // ucitaj inicijalne poslove pre paljenja worker-a
        foreach (var job in config.InitialJobs)
            EnqueueOrReject(job);

        workers = new Task[config.WorkerCount];
        for (int i = 0; i < config.WorkerCount; i++)
            workers[i] = Task.Run(() => WorkerLoop(shutdown.Token));

        // izvestaj svake minute
        reportTimer = new Timer(_ => SafeGenerateReport(), null,
            TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
    }

    public JobHandle Submit(Job job)
    {
        var handle = EnqueueOrReject(job);
        if (handle == null)
            throw new InvalidOperationException("Red je popunjen, posao je odbijen.");
        return handle;
    }

    public IEnumerable<Job> GetTopJobs(int n)
    {
        if (n <= 0) return new List<Job>();

        lock (queueGate)
        {
            return queue.UnorderedItems
                .OrderBy(x => x.Priority)
                .Take(n)
                .Select(x => x.Element)
                .ToList();
        }
    }

    public Job GetJob(Guid id)
    {
        lock (queueGate)
        {
            if (jobsById.ContainsKey(id))
                return jobsById[id];
        }
        throw new KeyNotFoundException("Posao sa Id " + id + " nije pronadjen.");
    }

    public int PendingCount
    {
        get { lock (queueGate) return pending; }
    }

    public string GenerateReport()
    {
        ExecutionRecord[] snapshot;
        lock (executions) snapshot = executions.ToArray();
        return reports.Generate(snapshot);
    }

    private JobHandle? EnqueueOrReject(Job job)
    {
        lock (queueGate)
        {
            // idempotentnost
            if (handles.ContainsKey(job.Id))
                return handles[job.Id];

            if (pending >= maxQueueSize)
                return null;

            var handle = new JobHandle(job.Id);
            handles[job.Id] = handle;
            jobsById[job.Id] = job;
            queue.Enqueue(job, job.Priority);
            pending++;

            signal.Release();
            return handle;
        }
    }

    private async Task WorkerLoop(CancellationToken shutdownToken)
    {
        while (!shutdownToken.IsCancellationRequested)
        {
            try { await signal.WaitAsync(shutdownToken); }
            catch (OperationCanceledException) { return; }

            Job? job = null;
            JobHandle? handle = null;
            lock (queueGate)
            {
                if (queue.TryDequeue(out var dequeued, out _))
                {
                    job = dequeued;
                    pending--;
                    if (handles.ContainsKey(job.Id))
                        handle = handles[job.Id];
                }
            }
            if (job == null || handle == null) continue;

            await RunWithRetries(job, handle, shutdownToken);
        }
    }

    private async Task RunWithRetries(Job job, JobHandle handle, CancellationToken shutdownToken)
    {
        Exception? lastError = null;
        TimeSpan totalElapsed = TimeSpan.Zero;

        for (int attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            if (shutdownToken.IsCancellationRequested)
            {
                handle.TryAbort(new OperationCanceledException("Sistem se gasi."));
                return;
            }

            using var attemptCts = CancellationTokenSource.CreateLinkedTokenSource(shutdownToken);
            attemptCts.CancelAfter(AttemptTimeout);

            var sw = Stopwatch.StartNew();
            try
            {
                int result = await Task.Run(
                    () => Dispatch(job, attemptCts.Token),
                    attemptCts.Token);
                sw.Stop();
                totalElapsed += sw.Elapsed;

                lock (executions)
                {
                    executions.Add(new ExecutionRecord(
                        job.Id, job.Type, JobOutcome.Completed, sw.Elapsed, DateTime.Now));
                }

                handle.TryComplete(result);
                RaiseCompleted(job, result, sw.Elapsed);
                return;
            }
            catch (OperationCanceledException) when (shutdownToken.IsCancellationRequested)
            {
                handle.TryAbort(new OperationCanceledException("Sistem se gasi."));
                return;
            }
            catch (Exception ex)
            {
                sw.Stop();
                totalElapsed += sw.Elapsed;
                lastError = ex;

                string reason = ex is OperationCanceledException ? "TIMEOUT" : "ERROR";
                RaiseFailed(job, attempt, reason, ex);

                if (attempt == MaxAttempts)
                {
                    lock (executions)
                    {
                        executions.Add(new ExecutionRecord(
                            job.Id, job.Type, JobOutcome.Aborted, totalElapsed, DateTime.Now));
                    }
                    handle.TryAbort(lastError);
                    return;
                }
            }
        }
    }

    private static int Dispatch(Job job, CancellationToken ct)
    {
        switch (job.Type)
        {
            case JobType.Prime: return PrimeJob.Execute(job.Payload, ct);
            case JobType.IO: return IoJob.Execute(job.Payload, ct);
            default: throw new NotSupportedException("Unknown JobType: " + job.Type);
        }
    }

    private void RaiseCompleted(Job job, int result, TimeSpan elapsed)
    {
        try { JobCompleted?.Invoke(this, new JobCompletedEventArgs(job, result, elapsed)); }
        catch { }
    }

    private void RaiseFailed(Job job, int attempt, string reason, Exception cause)
    {
        try { JobFailed?.Invoke(this, new JobFailedEventArgs(job, attempt, reason, cause)); }
        catch { }
    }

    private void SafeGenerateReport()
    {
        try
        {
            ExecutionRecord[] snapshot;
            lock (executions) snapshot = executions.ToArray();
            reports.Generate(snapshot);
        }
        catch { }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;

        try { reportTimer.Dispose(); } catch { }
        try { shutdown.Cancel(); } catch { }
        try { Task.WaitAll(workers, TimeSpan.FromSeconds(5)); } catch { }

        signal.Dispose();
        shutdown.Dispose();
    }
}

public class JobCompletedEventArgs : EventArgs
{
    public Job Job;
    public int Result;
    public TimeSpan Elapsed;

    public JobCompletedEventArgs(Job job, int result, TimeSpan elapsed)
    {
        Job = job;
        Result = result;
        Elapsed = elapsed;
    }
}

public class JobFailedEventArgs : EventArgs
{
    public Job Job;
    public int Attempt;
    public string Reason;
    public Exception Cause;

    public JobFailedEventArgs(Job job, int attempt, string reason, Exception cause)
    {
        Job = job;
        Attempt = attempt;
        Reason = reason;
        Cause = cause;
    }
}
