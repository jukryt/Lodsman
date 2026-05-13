using System.Runtime.InteropServices;

namespace Lodsman.Shutdown;

internal class ShutdownProcessor : IDisposable
{
    private readonly PosixSignalRegistration[] _signalRegistration;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly TaskCompletionSource _endWorkAwaiter;
    private readonly TaskCompletionSource<int> _exitAppAwaiter;
    private readonly IShutdownAction _action;

    private bool _isShutdown = false;

    public ShutdownProcessor(IShutdownAction action)
    {
        _signalRegistration =
        [
            PosixSignalRegistration.Create(PosixSignal.SIGINT, Shutdown),
            PosixSignalRegistration.Create(PosixSignal.SIGQUIT, Shutdown),
            PosixSignalRegistration.Create(PosixSignal.SIGHUP, Shutdown),
            PosixSignalRegistration.Create(PosixSignal.SIGTERM, Shutdown),
        ];

        _cancellationTokenSource = new CancellationTokenSource();
        _endWorkAwaiter = new TaskCompletionSource();
        _exitAppAwaiter = new TaskCompletionSource<int>();
        _action = action;
    }

    public CancellationToken CancellationToken => _cancellationTokenSource.Token;
    public Task EndWorkAwaiter => _endWorkAwaiter.Task;
    public Task<int> ExitAppAwaiter => _exitAppAwaiter.Task;

    private void Shutdown(PosixSignalContext? context)
    {
        if (context != null)
            context.Cancel = true;

        if (Interlocked.CompareExchange(ref _isShutdown, true, false))
            return;

        _cancellationTokenSource.Cancel();
        _endWorkAwaiter.TrySetResult();

        try
        {
            _action.ShutdownAsync().GetAwaiter().GetResult();
            _exitAppAwaiter.TrySetResult(0);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Shutdown. Error: {ex.GetType().Name} - {ex.Message}");
            _exitAppAwaiter.TrySetResult(ex.HResult);
        }
    }

    public void Dispose()
    {
        foreach (var signalRegistration in _signalRegistration)
            signalRegistration.Dispose();
        _cancellationTokenSource.Dispose();
    }
}
