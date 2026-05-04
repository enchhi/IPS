namespace IPS;

public class JobHandle
{
    private TaskCompletionSource<int> tcs;

    public Guid Id { get; set; }
    public Task<int> Result { get { return tcs.Task; } }

    public JobHandle(Guid id)
    {
        Id = id;
        tcs = new TaskCompletionSource<int>();
    }

    public bool TryComplete(int result)
    {
        return tcs.TrySetResult(result);
    }

    public bool TryAbort(Exception? cause)
    {
        return tcs.TrySetException(new JobAbortedException(Id, cause));
    }
}
