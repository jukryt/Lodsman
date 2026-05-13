using DotMake.CommandLine;
using Lodsman;

return await Cli.RunAsync<AppRunner>(args, new CliSettings { EnableDefaultExceptionHandler = true });

[CliCommand(
    NamePrefixConvention = CliNamePrefixConvention.DoubleHyphen,
    ShortFormPrefixConvention = CliNamePrefixConvention.SingleHyphen,
    NameCasingConvention = CliNameCasingConvention.KebabCase,
    TreatUnmatchedTokensAsErrors = false)]
internal class AppRunner : ICliRunAsyncWithReturn, IConfig
{
    [CliOption(Required = true, Arity = CliArgumentArity.ExactlyOne, HelpName = "Keenetic address")]
    public required string KeenAddress { get; set; }

    [CliOption(Required = true, Arity = CliArgumentArity.ExactlyOne, HelpName = "Keenetic user name")]
    public required string KeenUser { get; set; }

    [CliOption(Required = true, Arity = CliArgumentArity.ExactlyOne, HelpName = "Keenetic password")]
    public required string KeenPassword { get; set; }

    [CliOption(Required = true, Arity = CliArgumentArity.ExactlyOne, HelpName = "Keenetic route list name")]
    public required string KeenListName { get; set; }

    [CliOption(Required = true, Arity = CliArgumentArity.OneOrMore, HelpName = "Process name")]
    public required List<string> ProcessName { get; set; }

    public List<string> ProcessNames => ProcessName;

    [CliOption(Required = false, Arity = CliArgumentArity.ZeroOrOne)]
    public required bool ClearBeforeExit { get; set; } = false;

    public async Task<int> RunAsync()
    {
        return await new App(this).RunAsync();
    }
}
