namespace IPS.Processing;

public static class PrimeJob
{
    public const int MinThreads = 1;
    public const int MaxThreads = 8;

    public static int Execute(string payload, CancellationToken ct)
    {
        var (limit, threads) = Parse(payload);
        if (limit < 2) return 0;

        // podeli opseg [2, limit] na 'threads' delove
        int range = limit - 1;
        int chunk = (range + threads - 1) / threads;

        var tasks = new Task<int>[threads];
        for (int i = 0; i < threads; i++)
        {
            int from = 2 + i * chunk;
            int to = Math.Min(from + chunk - 1, limit);
            tasks[i] = Task.Run(() => CountInRange(from, to, ct), ct);
        }

        Task.WaitAll(tasks, ct);
        ct.ThrowIfCancellationRequested();

        int total = 0;
        for (int i = 0; i < tasks.Length; i++)
            total += tasks[i].Result;
        return total;
    }

    public static (int limit, int threads) Parse(string payload)
    {
        var fields = PayloadParser.Parse(payload);
        int n = PayloadParser.ReadInt(fields, "numbers");
        int t = PayloadParser.ReadInt(fields, "threads");

        if (t < MinThreads) t = MinThreads;
        if (t > MaxThreads) t = MaxThreads;
        return (n, t);
    }

    private static int CountInRange(int from, int to, CancellationToken ct)
    {
        if (from > to) return 0;
        int count = 0;
        for (int k = from; k <= to; k++)
        {
            if ((k & 0x3FF) == 0) ct.ThrowIfCancellationRequested();
            if (IsPrime(k)) count++;
        }
        return count;
    }

    private static bool IsPrime(int n)
    {
        if (n < 2) return false;
        if (n < 4) return true;
        if (n % 2 == 0) return false;
        if (n % 3 == 0) return false;
        for (int i = 5; (long)i * i <= n; i += 6)
        {
            if (n % i == 0) return false;
            if (n % (i + 2) == 0) return false;
        }
        return true;
    }
}
