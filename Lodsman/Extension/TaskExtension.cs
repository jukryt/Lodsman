namespace Lodsman.Extension;

internal static class TaskExtension
{
    public static async Task AwaitTokenAsync(CancellationToken cancellationToken)
    {
        try
        {
            var taskCompletionSource = new TaskCompletionSource();
            await using (cancellationToken.Register(() => { taskCompletionSource.TrySetResult(); }))
                await taskCompletionSource.Task;
        }
        catch (OperationCanceledException)
        {
        }
    }

    public static async Task IgnoreException(this Task task)
    {
        try
        {
            await task;
        }
        catch (Exception)
        {
            // ignore
        }
    }
}
