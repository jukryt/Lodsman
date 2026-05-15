using Lodsman.Context;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Lodsman.Main;

internal class ServiceWorker : BackgroundService
{
    public static async Task RunAsync(IContext context)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.Configure<HostOptions>(o => {
            o.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.StopHost;
            o.StartupTimeout = TimeSpan.FromSeconds(30);
            o.ShutdownTimeout = TimeSpan.FromSeconds(30);
        });
        builder.Services.AddWindowsService(o => o.ServiceName = context.ServiceName);
        builder.Services.AddHostedService(_ => new ServiceWorker(new AppExecutor(context)));

        var host = builder.Build();
        await host.RunAsync();
    }

    private readonly AppExecutor _appExecutor;

    private ServiceWorker(AppExecutor appExecutor)
    {
        _appExecutor = appExecutor;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await _appExecutor.ExecuteAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _appExecutor.Context.Log.Error(ex);
            Environment.Exit(ex.HResult);
        }
    }
}
