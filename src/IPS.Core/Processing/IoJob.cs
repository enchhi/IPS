namespace IPS.Processing;

public static class IoJob
{
    private const int SliceMs = 50;

    public static int Execute(string payload, CancellationToken ct)
    {
        int delay = ParseDelay(payload);

        // Thread.Sleep u manjim delovima da timeout moze da prekine cekanje
        int remaining = delay;
        while (remaining > 0)
        {
            int slice = remaining < SliceMs ? remaining : SliceMs;
            Thread.Sleep(slice);
            ct.ThrowIfCancellationRequested();
            remaining -= slice;
        }

        return Random.Shared.Next(0, 101);
    }

    public static int ParseDelay(string payload)
    {
        var fields = PayloadParser.Parse(payload);
        int delay = PayloadParser.ReadInt(fields, "delay");
        if (delay < 0) throw new FormatException("delay must be non-negative.");
        return delay;
    }
}
