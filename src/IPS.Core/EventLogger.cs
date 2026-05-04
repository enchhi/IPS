namespace IPS;

public class EventLogger : IDisposable
{
    private StreamWriter writer;
    private object gate = new object();
    private bool disposed;

    public EventLogger(string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);
        writer = new StreamWriter(path, append: true);
        writer.AutoFlush = true;
    }

    public Task WriteAsync(string status, Guid jobId, object? result)
    {
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{status}] {jobId}, {result}";
        return Task.Run(() =>
        {
            lock (gate)
            {
                if (disposed) return;
                writer.WriteLine(line);
            }
        });
    }

    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;
            disposed = true;
            writer.Dispose();
        }
    }
}
