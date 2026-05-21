using DotMake.CommandLine;
using Lodsman.Context;
using Lodsman.Context.Router.Keenetic;
using Lodsman.Log;

namespace Lodsman.CliRunner;

[CliCommand(Name = "/keenetic", Alias = "/keen", Description = "Router Keenetic(Netcraze)",
    Parent = typeof(RootCommand),
    NamePrefixConvention = CliNamePrefixConvention.DoubleHyphen,
    ShortFormPrefixConvention = CliNamePrefixConvention.SingleHyphen,
    NameCasingConvention = CliNameCasingConvention.KebabCase,
    TreatUnmatchedTokensAsErrors = false)]
internal class KeeneticCommand : BaseCommand, IKeeneticConfig
{
    [CliOption(Alias = "-a", Required = true, Arity = CliArgumentArity.ExactlyOne, HelpName = "ip address")]
    public required string Address { get; set; }

    [CliOption(Alias = "-u", Required = true, Arity = CliArgumentArity.ExactlyOne, HelpName = "user name")]
    public required string User { get; set; }

    [CliOption(Alias = "-p", Required = true, Arity = CliArgumentArity.ExactlyOne, HelpName = "password")]
    public required string Password { get; set; }

    [CliOption(Alias = "-ln", Required = true, Arity = CliArgumentArity.OneOrMore, HelpName = "dns route list name")]
    public required List<string> ListName { get; set; }

    public List<string> ListNames => ListName;

    public override async Task<IContext> BuildContextAsync(ILog log, CancellationToken cancellationToken)
    {
        var api = new KeeneticApi(HttpClientHelper.Instance, Address, User, Password);
        var routes = await RetryGetDomainRoutesAsync(api, log, cancellationToken);

        return new KeeneticContext(this, api, routes.ToArray(), log);
    }

    protected override IReadOnlyCollection<string> GetServiceArguments()
    {
        var arguments = new List<string>
        {
            "/keen",
            $"-a \"{Address}\"",
            $"-u \"{User}\"",
            $"-p \"{Password}\"",
        };

        arguments.AddRange(ListNames.Select(listName => $"-ln \"{listName}\""));

        return arguments;
    }

    private async Task<IReadOnlyCollection<DomainRoute>> RetryGetDomainRoutesAsync(KeeneticApi api, ILog log, CancellationToken cancellationToken)
    {
        while (true)
        {
            var result = new List<DomainRoute>();

            try
            {
                foreach (var listName in ListNames)
                    result.Add(await api.GetDomainRouteAsync(listName, cancellationToken));

                return result;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
                await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            }
        }
    }
}
