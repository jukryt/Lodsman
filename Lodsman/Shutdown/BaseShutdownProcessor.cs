using Lodsman.Log;

namespace Lodsman.Shutdown;

internal abstract class BaseShutdownProcessor : IShutdownProcessor
{
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly TaskCompletionSource _endWorkAwaiter;
    private readonly TaskCompletionSource<int> _exitAppAwaiter;
    private readonly IShutdownAction _action;
    protected readonly ILog Log;

    private bool _isShutdown = false;

    protected BaseShutdownProcessor(IShutdownAction action, ILog log)
    {
        _cancellationTokenSource = new CancellationTokenSource();
        _endWorkAwaiter = new TaskCompletionSource();
        _exitAppAwaiter = new TaskCompletionSource<int>();
        _action = action;
        Log = log;
    }

    public CancellationToken CancellationToken => _cancellationTokenSource.Token;
    public Task EndWorkAwaiter => _endWorkAwaiter.Task;
    public Task<int> ExitAppAwaiter => _exitAppAwaiter.Task;

    protected void Shutdown()
    {
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
            Log.Error(ex);
            _exitAppAwaiter.TrySetResult(ex.HResult);
        }
    }

    public virtual void Dispose()
    {
        _cancellationTokenSource.Dispose();
    }
}
