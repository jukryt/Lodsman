using System.Reflection;
using DotMake.CommandLine;
using Lodsman.AddressSaver;
using Lodsman.Context;
using Lodsman.Shutdown;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;

namespace Lodsman.CliRunner;

internal abstract class BaseAppRunner : RootAppRunner, IConfig
{
    [CliOption(Alias = "-is", Required = false, Arity = CliArgumentArity.ZeroOrOne)]
    public required bool InstallService { get; set; } = false;

    [CliOption(Alias = "-us", Required = false, Arity = CliArgumentArity.ZeroOrOne)]
    public required bool UninstallService { get; set; } = false;

    [CliOption(Alias = "-pn", Required = true, Arity = CliArgumentArity.OneOrMore, HelpName = "Process name")]
    public required List<string> ProcessName { get; set; }

    public List<string> ProcessNames => ProcessName;

    [CliOption(Alias = "-cbe", Required = false, Arity = CliArgumentArity.ZeroOrOne)]
    public required bool ClearBeforeExit { get; set; } = false;

    protected async Task<int> RunAsync(IContext context)
    {
        if (InstallService)
            return await InstallServiceAsync(context);

        if (UninstallService)
            return await UninstallServiceAsync(context);

        return WindowsServiceHelpers.IsWindowsService()
            ? await RunAsServiceAsync(context)
            : await RunAsConsoleAsync(context);
    }

    protected abstract string[] GetServiceArguments();

    private async Task<int> InstallServiceAsync(IContext context)
    {
        var servicePath = Environment.ProcessPath ?? Assembly.GetExecutingAssembly().Location;

        var serviceArguments = new List<string>(GetServiceArguments());
        serviceArguments.AddRange(ProcessNames.Select(processName => $"-pn \"{processName}\""));
        if (ClearBeforeExit) serviceArguments.Add("-cbe");

        serviceArguments = serviceArguments.Select(x => x.Replace("\"", "\\\"")).ToList();
        var installArguments = $"/c sc create \"{context.ServiceName}\" binPath= \"\\\"{servicePath}\\\" {string.Join(" ", serviceArguments)}\" start= auto";
        var installResult = await ProcessHelper.ExecuteAsync("cmd", installArguments, context.Log);
        if (installResult != 0 && installResult != 1073)
            return installResult;

        var startArguments = $"/c sc start \"{context.ServiceName}\"";
        return await ProcessHelper.ExecuteAsync("cmd", startArguments, context.Log);
    }

    private async Task<int> UninstallServiceAsync(IContext context)
    {
        var stopArguments = $"/c sc stop \"{context.ServiceName}\"";
        await ProcessHelper.ExecuteAsync("cmd", stopArguments, context.Log);

        var deleteArguments = $"/c sc delete \"{context.ServiceName}\"";
        return await ProcessHelper.ExecuteAsync("cmd", deleteArguments, context.Log);
    }

    private async Task<int> RunAsServiceAsync(IContext context)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.Configure<HostOptions>(options =>
        {
            options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.StopHost;
        });
        builder.Services.AddWindowsService(o => o.ServiceName = context.ServiceName);
        builder.Services.AddHostedService(_ => new ServiceWorker(context));
        var host = builder.Build();
        await host.RunAsync();
        return 0;
    }

    private async Task<int> RunAsConsoleAsync(IContext context)
    {
        Console.Title = context.ServiceName;

        try
        {
            using var shutdownProcessor = new PosixShutdownProcessor(context.ShutdownAction, context.Log);
            var addressSaverProcessor = new AddressSaverProcessor(context.AddressSaverAction, context.Log, shutdownProcessor.CancellationToken);
            var app = new App(context, addressSaverProcessor, shutdownProcessor);

            return await app.RunAsync();
        }
        catch (Exception ex)
        {
            context.Log.Error(ex);
            return ex.HResult;
        }
    }

    internal class ServiceWorker(IContext context) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                using var shutdownProcessor = new CancellationTokenShutdownProcessor(context.ShutdownAction, context.Log, stoppingToken);
                var addressSaverProcessor = new AddressSaverProcessor(context.AddressSaverAction, context.Log, shutdownProcessor.CancellationToken);
                var app = new App(context, addressSaverProcessor, shutdownProcessor);

                await app.RunAsync();
            }
            catch (Exception ex)
            {
                context.Log.Error(ex);
                Environment.Exit(ex.HResult);
            }
        }
    }
}
