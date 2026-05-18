using System.Reflection;
using DotMake.CommandLine;
using Lodsman.Context;
using Lodsman.Log;
using Lodsman.Main;
using Microsoft.Extensions.Hosting.WindowsServices;

namespace Lodsman.CliRunner;

internal abstract class BaseCommand : RootCommand, ICliRunAsyncWithReturn, IConfig
{
    public bool IsService => WindowsServiceHelpers.IsWindowsService();

    public string ServiceName => $"{App.Name} - {string.Join(", ", ProcessNames.Order().ToHashSet(StringComparer.OrdinalIgnoreCase))}";

    [CliOption(Alias = "-is", Required = false, Arity = CliArgumentArity.ZeroOrOne)]
    public required bool InstallService { get; set; } = false;

    [CliOption(Alias = "-us", Required = false, Arity = CliArgumentArity.ZeroOrOne)]
    public required bool UninstallService { get; set; } = false;

    [CliOption(Alias = "-pn", Required = true, Arity = CliArgumentArity.OneOrMore, HelpName = "Process name")]
    public required List<string> ProcessName { get; set; }

    public List<string> ProcessNames => ProcessName;

    [CliOption(Alias = "-cbe", Required = false, Arity = CliArgumentArity.ZeroOrOne)]
    public required bool ClearBeforeExit { get; set; } = false;

    public async Task<int> RunAsync()
    {
        await using var log = CreateLog();

        if (InstallService)
            return await InstallServiceAsync(log);

        if (UninstallService)
            return await UninstallServiceAsync(log);

        return IsService
            ? await RunAsServiceAsync(log)
            : await RunAsConsoleAsync(log);
    }

    public abstract Task<IContext> BuildContextAsync(ILog log, CancellationToken cancellationToken);

    protected abstract string[] GetServiceArguments();

    private async Task<int> InstallServiceAsync(ILog log)
    {
        var servicePath = Environment.ProcessPath ?? Assembly.GetExecutingAssembly().Location;

        var serviceArguments = new List<string>(GetServiceArguments());
        serviceArguments.AddRange(ProcessNames.Select(processName => $"-pn \"{processName}\""));
        if (ClearBeforeExit) serviceArguments.Add("-cbe");

        serviceArguments = serviceArguments.Select(x => x.Replace("\"", "\\\"")).ToList();
        var installArguments = $"/c sc create \"{ServiceName}\" binPath= \"\\\"{servicePath}\\\" {string.Join(" ", serviceArguments)}\" start= auto";
        var installResult = await ProcessHelper.ExecuteAsync("cmd", installArguments, log);
        if (installResult != 0 && installResult != 1073)
            return installResult;

        var startArguments = $"/c sc start \"{ServiceName}\"";
        return await ProcessHelper.ExecuteAsync("cmd", startArguments, log);
    }

    private async Task<int> UninstallServiceAsync(ILog log)
    {
        var stopArguments = $"/c sc stop \"{ServiceName}\"";
        await ProcessHelper.ExecuteAsync("cmd", stopArguments, log);

        var deleteArguments = $"/c sc delete \"{ServiceName}\"";
        return await ProcessHelper.ExecuteAsync("cmd", deleteArguments, log);
    }

    private async Task<int> RunAsServiceAsync(ILog log)
    {
        await ServiceAppExecutor.ExecuteAsync(this, log);
        return 0;
    }

    private async Task<int> RunAsConsoleAsync(ILog log)
    {
        return await ConsoleAppExecutor.ExecuteAsync(this, log);
    }

    private ILog CreateLog()
    {
        if (IsService)
        {
            var logFileName = FileSystemHelper.NormalizeFileName($"{ServiceName}.log");
            var logFilePath = Path.Combine(FileSystemHelper.GetAppDataFolder(), logFileName);
            return new FileLog(logFilePath);
        }

        return new ConsoleLog();
    }
}
