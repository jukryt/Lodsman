using DotMake.CommandLine;

return await Cli.RunAsync<RootCommand>(args, new CliSettings { EnableDefaultExceptionHandler = true });

[CliCommand(Name = "Lodsman",
    NamePrefixConvention = CliNamePrefixConvention.DoubleHyphen,
    ShortFormPrefixConvention = CliNamePrefixConvention.SingleHyphen,
    NameCasingConvention = CliNameCasingConvention.KebabCase,
    TreatUnmatchedTokensAsErrors = false)]
internal class RootCommand;
