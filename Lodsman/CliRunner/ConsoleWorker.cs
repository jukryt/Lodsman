namespace Lodsman.CliRunner;

internal class ConsoleWorker(AppExecutor appExecutor)
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly ManualResetEventSlim _endWorkEvent = new();
    private bool _isShutdown;

    public async Task<int> ExecuteAsync()
    {
        try
        {
            AppDomain.CurrentDomain.ProcessExit += ProcessExit;
            return await appExecutor.ExecuteAsync(_cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            appExecutor.Context.Log.Error(ex);
            return ex.HResult;
        }
        finally
        {
            _endWorkEvent.Set();
        }
    }

    private void ProcessExit(object? sender, EventArgs e)
    {
        if (Interlocked.CompareExchange(ref _isShutdown, true, false))
            return;

        _cancellationTokenSource.Cancel();
        _endWorkEvent.Wait();
    }
}
