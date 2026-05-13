using Lodsman.Log;

namespace Lodsman.AddressSaver;

internal class AddressSaverProcessor(IAddressSaverAction action, ILog log, CancellationToken cancellationToken)
{
    private bool _isRunning = false;
    private ulong _counter = 0;

    public int MaxAddressCount => action.MaxAddressCount;

    public void Save(IReadOnlyCollection<string> addresses)
    {
        if (cancellationToken.IsCancellationRequested)
            return;

        SaveAsync(addresses, Interlocked.Increment(ref _counter));
    }

    private async void SaveAsync(IReadOnlyCollection<string> addresses, ulong counter)
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
            await action.SaveAsync(addresses, cancellationToken);

            if (Interlocked.Read(ref _counter) == counter)
                log.Info("Save complete");
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            log.Error(ex);
        }
        finally
        {
            Interlocked.Exchange(ref _isRunning, false);
        }
    }
}
