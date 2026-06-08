using System.Text.Json.Serialization;
using DotMake.CommandLine;
using Lodsman.Context;
using Lodsman.Context.Router.Keenetic;
using Lodsman.Helper;
using Lodsman.Json;
using Lodsman.Log;

namespace Lodsman.CliRunner;

[CliCommand(Name = "/keenetic", Alias = "/keen", Description = "Router Keenetic(Netcraze)",
    Parent = typeof(RootCommand),
    NamePrefixConvention = CliNamePrefixConvention.DoubleHyphen,
    ShortFormPrefixConvention = CliNamePrefixConvention.SingleHyphen,
    NameCasingConvention = CliNameCasingConvention.KebabCase,
    TreatUnmatchedTokensAsErrors = false)]
[JsonBaseType(BaseType = typeof(AppRunCommand), TypeDiscriminator = "keenetic")]
internal class KeeneticCommand : AppRunCommand, IKeeneticConfig
{
    [CliOption(Alias = "-a", Required = true, Arity = CliArgumentArity.ExactlyOne, HelpName = "ip address")]
    public required string Address { get; set; }

    [CliOption(Alias = "-u", Required = true, Arity = CliArgumentArity.ExactlyOne, HelpName = "user name")]
    public required string User { get; set; }

    [CliOption(Alias = "-p", Required = true, Arity = CliArgumentArity.ExactlyOne, HelpName = "password")]
    public required string Password { get; set; }

    [CliOption(Alias = "-ln", Required = true, Arity = CliArgumentArity.OneOrMore, HelpName = "dns route list name")]
    public required List<string> ListName { get; set; }

    [JsonIgnore]
    public List<string> ListNames => ListName;

    public override async Task<IContext> BuildContextAsync(ILog log, CancellationToken cancellationToken)
    {
        var api = new KeeneticApi(HttpClientHelper.Wrapper, Address, User, Password);
        var routes = await RetryGetDomainRoutesAsync(api, log, cancellationToken);

        return new KeeneticContext(this, api, routes.ToArray(), log);
    }

    protected override IEnumerable<string> GetServiceArguments()
    {
        yield return "/keen";
        yield return $"-a \"{Address}\"";
        yield return $"-u \"{User}\"";
        yield return $"-p \"{Password}\"";

        foreach (var listName in ListNames)
            yield return $"-ln \"{listName}\"";

        foreach (var argument in base.GetServiceArguments())
            yield return argument;
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
