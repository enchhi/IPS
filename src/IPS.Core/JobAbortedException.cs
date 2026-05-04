namespace IPS;

public class JobAbortedException : Exception
{
    public Guid JobId;

    public JobAbortedException(Guid jobId, Exception? cause)
        : base("Job aborted: " + jobId, cause)
    {
        JobId = jobId;
    }
}
