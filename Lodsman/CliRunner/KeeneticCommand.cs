using DotMake.CommandLine;
using Lodsman.Context.Router.Keenetic;

namespace Lodsman.CliRunner;

[CliCommand(Name = "/keenetic", Alias = "/keen", Description = "Router Keenetic",
    Parent = typeof(RootCommand),
    NamePrefixConvention = CliNamePrefixConvention.DoubleHyphen,
    ShortFormPrefixConvention = CliNamePrefixConvention.SingleHyphen,
    NameCasingConvention = CliNameCasingConvention.KebabCase,
    TreatUnmatchedTokensAsErrors = false)]
internal class KeeneticCommand : BaseCommand, ICliRunAsyncWithReturn, IKeeneticConfig
{
    [CliOption(Alias = "-a", Required = true, Arity = CliArgumentArity.ExactlyOne, HelpName = "Keenetic address")]
    public required string Address { get; set; }

    [CliOption(Alias = "-u", Required = true, Arity = CliArgumentArity.ExactlyOne, HelpName = "Keenetic user name")]
    public required string User { get; set; }

    [CliOption(Alias = "-p", Required = true, Arity = CliArgumentArity.ExactlyOne, HelpName = "Keenetic password")]
    public required string Password { get; set; }

    [CliOption(Alias = "-ln", Required = true, Arity = CliArgumentArity.ExactlyOne, HelpName = "Keenetic route list name")]
    public required string ListName { get; set; }

    public async Task<int> RunAsync()
    {
        await using var context = await KeeneticContext.BuildAsync(this);
        return await RunAsync(context);
    }

    protected override string[] GetServiceArguments()
    {
        return
        [
            "/k",
            $"-a \"{Address}\"",
            $"-u \"{User}\"",
            $"-p \"{Password}\"",
            $"-ln \"{ListName}\"",
        ];
    }
}
