using Lodsman.Log;

namespace Lodsman.Shutdown;

internal class CancellationTokenShutdownProcessor : BaseShutdownProcessor
{
    private readonly CancellationToken _cancellationToken;

    public CancellationTokenShutdownProcessor(IShutdownAction action, ILog log, CancellationToken cancellationToken) : base(action, log)
    {
        _cancellationToken = cancellationToken;
        LoopCheck();
    }

    private async void LoopCheck()
    {
        try
        {
            while (true)
            {
                if (_cancellationToken.IsCancellationRequested)
                    break;

                await Task.Delay(500, _cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }

        Shutdown();
    }
}
