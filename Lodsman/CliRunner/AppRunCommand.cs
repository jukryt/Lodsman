using System.Text.Json.Serialization;
using DotMake.CommandLine;
using Lodsman.AppExecutor;
using Lodsman.Context;
using Lodsman.Log;

namespace Lodsman.CliRunner;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
internal abstract class AppRunCommand : BaseCommand, IConfig
{
    [JsonIgnore]
    public override string Name { get; set; } = string.Empty;

    [CliOption(Alias = "-pn", Required = true, Arity = CliArgumentArity.OneOrMore, HelpName = "process name")]
    public required List<string> ProcessName { get; set; }

    [JsonIgnore]
    public List<string> ProcessNames => ProcessName;

    [CliOption(Alias = "-sd", Required = false, Arity = CliArgumentArity.ZeroOrOne, HelpName = "milliseconds")]
    public uint SavingDelay { get; set; } = 1000;

    [CliOption(Alias = "-sal", Required = false, Arity = CliArgumentArity.ZeroOrOne)]
    public bool ShowAddressesOnLoad { get; set; } = false;

    [CliOption(Alias = "-cbe", Required = false, Arity = CliArgumentArity.ZeroOrOne)]
    public bool ClearBeforeExit { get; set; } = false;

    [CliOption(Alias = "-scf", Required = false, Arity = CliArgumentArity.ZeroOrOne, HelpName = "config file path")]
    [JsonIgnore]
    public string SaveConfigFile { get; set; } = string.Empty;

    public override Task InitAsync(ILog log)
    {
        Name = $"{App.Name} - {string.Join(", ", ProcessNames.Order().ToHashSet(StringComparer.OrdinalIgnoreCase))}";
        return Task.CompletedTask;
    }

    public override async Task RunAsServiceAsync(ILog log)
    {
        await ServiceAppExecutor.ExecuteAsync(this, log);
    }

    public override async Task RunAsConsoleAsync(ILog log)
    {
        if (string.IsNullOrEmpty(SaveConfigFile))
            await ConsoleAppExecutor.ExecuteAsync(this, log);
        else
            await ConfigFileCommand.SaveConfigAsync(this, SaveConfigFile, log);
    }

    public abstract Task<IContext> BuildContextAsync(ILog log, CancellationToken cancellationToken);

    protected override IEnumerable<string> GetServiceArguments()
    {
        yield return $"-n \"{Name}\"";

        foreach (var processName in ProcessNames)
            yield return $"-pn \"{processName}\"";

        yield return $"-sd {SavingDelay}";

        if (ShowAddressesOnLoad)
            yield return "-sal";

        if (ClearBeforeExit)
            yield return "-cbe";
    }
}
