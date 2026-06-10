using Lodsman.Log;

namespace Lodsman.Helper;

internal class AsyncActionThrottler<T>(Func<T, CancellationToken, Task> action, TimeSpan throttlingTime, ILog? log = null)
{
    private bool _isRunning = false;
    private ulong _counter = 0;

    public event EventHandler? Complete;

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
                await Task.Delay(throttlingTime, cancellationToken);

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
                OnComplete();
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

    protected virtual void OnComplete()
    {
        Complete?.Invoke(this, EventArgs.Empty);
    }
}
