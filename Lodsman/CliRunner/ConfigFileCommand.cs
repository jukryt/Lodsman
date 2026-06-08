using DotMake.CommandLine;
using Lodsman.Json;
using Lodsman.Log;

namespace Lodsman.CliRunner;

[CliCommand(Name = "/file", Alias ="/f", Description = "Run from config file",
    Parent = typeof(RootCommand),
    NamePrefixConvention = CliNamePrefixConvention.DoubleHyphen,
    ShortFormPrefixConvention = CliNamePrefixConvention.SingleHyphen,
    NameCasingConvention = CliNameCasingConvention.KebabCase,
    TreatUnmatchedTokensAsErrors = false)]
internal class ConfigFileCommand : BaseCommand
{
    public override string Name { get; set; } = string.Empty;

    [CliOption(Alias = "-c", Required = true, Arity = CliArgumentArity.ExactlyOne, HelpName = "config file path")]
    public required string Config { get; set; }

    private AppRunCommand? _command;

    public override async Task InitAsync(ILog log)
    {
        var command = await CreateCommandAsync();
        if (command == null)
            throw new Exception("Incorrect config file.");

        await command.InitAsync(log);

        Name = command.Name;
        _command = command;
    }

    public override Task RunAsServiceAsync(ILog log) => _command?.RunAsServiceAsync(log) ?? Task.CompletedTask;
    public override Task RunAsConsoleAsync(ILog log) => _command?.RunAsConsoleAsync(log) ?? Task.CompletedTask;

    protected override IEnumerable<string> GetServiceArguments()
    {
        yield return "/f";
        yield return $"-c \"{Config}\"";
    }

    private async Task<AppRunCommand?> CreateCommandAsync()
    {
        await using var stream = File.OpenRead(Config);
        return await JsonHelper.DeserializeAsync<AppRunCommand>(stream);
    }

    public static async Task SaveConfigAsync(AppRunCommand command, string filePath, ILog log)
    {
        await using var stream = File.Create(filePath);
        await JsonHelper.SerializeAsync(stream, command);
        log.Info($"Config file saved to: {Path.GetFullPath(filePath)}");
    }
}
