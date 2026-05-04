namespace IPS;

public enum JobOutcome
{
    Completed,
    Aborted
}

public class ExecutionRecord
{
    public Guid JobId { get; set; }
    public JobType Type { get; set; }
    public JobOutcome Outcome { get; set; }
    public TimeSpan Duration { get; set; }
    public DateTime FinishedAt { get; set; }

    public ExecutionRecord(Guid jobId, JobType type, JobOutcome outcome, TimeSpan duration, DateTime finishedAt)
    {
        JobId = jobId;
        Type = type;
        Outcome = outcome;
        Duration = duration;
        FinishedAt = finishedAt;
    }
}
