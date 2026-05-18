using DotMake.CommandLine;
using Lodsman.Context;
using Lodsman.Context.Router.Keenetic;
using Lodsman.Log;

namespace Lodsman.CliRunner;

[CliCommand(Name = "/keenetic", Alias = "/keen", Description = "Router Keenetic",
    Parent = typeof(RootCommand),
    NamePrefixConvention = CliNamePrefixConvention.DoubleHyphen,
    ShortFormPrefixConvention = CliNamePrefixConvention.SingleHyphen,
    NameCasingConvention = CliNameCasingConvention.KebabCase,
    TreatUnmatchedTokensAsErrors = false)]
internal class KeeneticCommand : BaseCommand, IKeeneticConfig
{
    [CliOption(Alias = "-a", Required = true, Arity = CliArgumentArity.ExactlyOne, HelpName = "Keenetic address")]
    public required string Address { get; set; }

    [CliOption(Alias = "-u", Required = true, Arity = CliArgumentArity.ExactlyOne, HelpName = "Keenetic user name")]
    public required string User { get; set; }

    [CliOption(Alias = "-p", Required = true, Arity = CliArgumentArity.ExactlyOne, HelpName = "Keenetic password")]
    public required string Password { get; set; }

    [CliOption(Alias = "-ln", Required = true, Arity = CliArgumentArity.ExactlyOne, HelpName = "Keenetic route list name")]
    public required string ListName { get; set; }

    public override async Task<IContext> BuildContextAsync(ILog log, CancellationToken cancellationToken)
    {
        var api = new KeeneticApi(HttpClientHelper.Instance, Address, User, Password);
        var route = await RetryGetDomainRouteAsync(api, log, cancellationToken);

        return new KeeneticContext(this, api, route, log);
    }

    protected override string[] GetServiceArguments()
    {
        return
        [
            "/keen",
            $"-a \"{Address}\"",
            $"-u \"{User}\"",
            $"-p \"{Password}\"",
            $"-ln \"{ListName}\"",
        ];
    }

    private async Task<DomainRoute> RetryGetDomainRouteAsync(KeeneticApi api, ILog log, CancellationToken cancellationToken)
    {
        while (true)
        {
            try
            {
                return await api.GetDomainRouteAsync(ListName, cancellationToken);
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
