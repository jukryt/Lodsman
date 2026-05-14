using DotMake.CommandLine;

namespace Lodsman.CliRunner;

[CliCommand(Name = "Lodsman",
    NamePrefixConvention = CliNamePrefixConvention.DoubleHyphen,
    ShortFormPrefixConvention = CliNamePrefixConvention.SingleHyphen,
    NameCasingConvention = CliNameCasingConvention.KebabCase,
    TreatUnmatchedTokensAsErrors = false)]
internal class RootAppRunner;
