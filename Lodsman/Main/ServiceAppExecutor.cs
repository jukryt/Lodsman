using Lodsman.Context;
using Lodsman.Log;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Lodsman.Main;

internal class ServiceAppExecutor : BackgroundService
{
    public static async Task ExecuteAsync(IConfig config, ILog log)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.Configure<HostOptions>(o => {
            o.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.StopHost;
            o.StartupTimeout = TimeSpan.FromSeconds(30);
            o.ShutdownTimeout = TimeSpan.FromSeconds(30);
        });
        builder.Services.AddWindowsService(o => o.ServiceName = config.ServiceName);
        builder.Services.AddHostedService(_ => new ServiceAppExecutor(config, log));

        var host = builder.Build();
        await host.RunAsync();
    }

    private readonly IConfig _config;
    private readonly ILog _log;

    private ServiceAppExecutor(IConfig config, ILog log)
    {
        _config = config;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var appExecutor = new AppExecutor(_config, _log);
            await appExecutor.ExecuteAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _log.Error(ex);
            Environment.Exit(ex.HResult);
        }
    }
}
