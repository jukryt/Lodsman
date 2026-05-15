using Lodsman.Context;

namespace Lodsman.Main;

internal class ConsoleWorker
{
    public static async Task<int> RunAsync(IContext context)
    {
        Console.Title = context.ServiceName;
        return await new ConsoleWorker(new AppExecutor(context)).ExecuteAsync();
    }

    private readonly AppExecutor _appExecutor;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly ManualResetEventSlim _endWorkEvent = new();
    private bool _isShutdown;

    private ConsoleWorker(AppExecutor appExecutor)
    {
        _appExecutor = appExecutor;
    }

    public async Task<int> ExecuteAsync()
    {
        try
        {
            AppDomain.CurrentDomain.ProcessExit += ProcessExit;
            return await _appExecutor.ExecuteAsync(_cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            _appExecutor.Context.Log.Error(ex);
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

        _cancellationTokenSource.CancelAsync();
        _endWorkEvent.Wait();
    }
}
