using Lodsman.Log;

namespace Lodsman;

internal class AsyncActionThrottler<T>(Func<T, CancellationToken, Task> action, Action? actionComplete = null, ILog? log = null)
{
    private bool _isRunning = false;
    private ulong _counter = 0;

    public void Run(T data, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return;

        RunAsync(data, Interlocked.Increment(ref _counter), cancellationToken);
    }

    private async void RunAsync(T data, ulong counter, CancellationToken cancellationToken)
    {
        try
        {
            do
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);

                if (Interlocked.Read(ref _counter) != counter)
                    return;

            } while (Interlocked.CompareExchange(ref _isRunning, true, false));
        }
        catch (OperationCanceledException)
        {
            return;
        }

        try
        {
            await action(data, cancellationToken);

            if (Interlocked.Read(ref _counter) == counter)
                actionComplete?.Invoke();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            log?.Error(ex);
        }
        finally
        {
            Interlocked.Exchange(ref _isRunning, false);
        }
    }
}
