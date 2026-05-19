using Lodsman.Context;
using Lodsman.Log;

namespace Lodsman.Main;

internal class ConsoleAppExecutor
{
    public static async Task<int> ExecuteAsync(IConfig config, ILog log)
    {
        Console.Title = config.ServiceName;
        return await new ConsoleAppExecutor(config, log).ExecuteAsync();
    }

    private readonly IConfig _config;
    private readonly ILog _log;
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly ManualResetEventSlim _endWorkEvent = new();
    private bool _isShutdown;

    private ConsoleAppExecutor(IConfig config, ILog log)
    {
        _config = config;
        _log = log;
    }

    private async Task<int> ExecuteAsync()
    {
        try
        {
            var appExecutor = new AppExecutor(_config, _log);
            AppDomain.CurrentDomain.ProcessExit += ProcessExit;
            await appExecutor.ExecuteAsync(_cancellationTokenSource.Token);
            return 0;
        }
        catch (OperationCanceledException)
        {
            return 0;
        }
        catch (Exception ex)
        {
            _log.Error(ex);
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
