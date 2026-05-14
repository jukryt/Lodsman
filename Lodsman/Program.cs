using DotMake.CommandLine;
using Lodsman;
using Lodsman.AddressSaver;
using Lodsman.Log;
using Lodsman.Shutdown;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;

return await Cli.RunAsync<AppRunner>(args, new CliSettings { EnableDefaultExceptionHandler = true });

[CliCommand(
    NamePrefixConvention = CliNamePrefixConvention.DoubleHyphen,
    ShortFormPrefixConvention = CliNamePrefixConvention.SingleHyphen,
    NameCasingConvention = CliNameCasingConvention.KebabCase,
    TreatUnmatchedTokensAsErrors = false)]
internal class AppRunner : ICliRunAsyncWithReturn, IConfig
{
    [CliOption(Required = true, Arity = CliArgumentArity.ExactlyOne, HelpName = "Keenetic address")]
    [CliOption(Alias = "-ka", Required = true, Arity = CliArgumentArity.ExactlyOne, HelpName = "Keenetic address")]
    public required string KeenAddress { get; set; }

    [CliOption(Alias = "-ku", Required = true, Arity = CliArgumentArity.ExactlyOne, HelpName = "Keenetic user name")]
    public required string KeenUser { get; set; }

    [CliOption(Alias = "-kp", Required = true, Arity = CliArgumentArity.ExactlyOne, HelpName = "Keenetic password")]
    public required string KeenPassword { get; set; }

    [CliOption(Alias = "-kln", Required = true, Arity = CliArgumentArity.ExactlyOne, HelpName = "Keenetic route list name")]
    public required string KeenListName { get; set; }

    [CliOption(Alias = "-pn", Required = true, Arity = CliArgumentArity.OneOrMore, HelpName = "Process name")]
    public required List<string> ProcessName { get; set; }

    public List<string> ProcessNames => ProcessName;

    [CliOption(Alias = "-cbe", Required = false, Arity = CliArgumentArity.ZeroOrOne)]
    public required bool ClearBeforeExit { get; set; } = false;

    public Task<int> RunAsync()
    {
        var processNames = ProcessNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var name = $"{App.Name} - {string.Join(", ", processNames)}";

        return WindowsServiceHelpers.IsWindowsService()
            ? RunAsServiceAsync(name)
            : RunAsConsoleAsync(name);
    }

    private async Task<int> RunAsServiceAsync(string serviceName)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.Configure<HostOptions>(options =>
        {
            options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.StopHost;
        });
        builder.Services.AddWindowsService(o => o.ServiceName = serviceName);
        builder.Services.AddHostedService(_ => new ServiceWorker(this, serviceName));
        var host = builder.Build();
        await host.RunAsync();
        return 0;
    }

    private async Task<int> RunAsConsoleAsync(string name)
    {
        Console.Title = name;
        var log = new ConsoleLog();

        try
        {
            using var context = await ContextBuilder.BuildAsync(this, log);
            using var shutdownProcessor = new PosixShutdownProcessor(context.ShutdownAction, log);
            var addressSaverProcessor = new AddressSaverProcessor(context.AddressSaverAction, log, shutdownProcessor.CancellationToken);
            var app = new App(context, addressSaverProcessor, shutdownProcessor, log);

            return await app.RunAsync();
        }
        catch (Exception ex)
        {
            log.Error(ex);
            return ex.HResult;
        }
    }
}
