using Microsoft.Extensions.Hosting;

namespace Lodsman.CliRunner;

internal class ServiceWorker(AppExecutor appExecutor) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await appExecutor.ExecuteAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            appExecutor.Context.Log.Error(ex);
            Environment.Exit(ex.HResult);
        }
    }
}
