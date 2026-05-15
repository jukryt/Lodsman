namespace Lodsman.Context.Router.Keenetic;

internal class KeeneticContext : BaseContext
{
    public static async Task<KeeneticContext> BuildAsync(IKeeneticConfig config)
    {
        var keeneticApi = new KeeneticApi(HttpClientHelper.Instance, config.Address, config.User, config.Password);
        var route = await keeneticApi.GetDomainRouteAsync(config.ListName);

        return new KeeneticContext(config, keeneticApi, route);
    }

    private readonly IKeeneticConfig _config;
    private readonly KeeneticApi _keeneticApi;
    private readonly DomainRoute _route;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private KeeneticContext(IKeeneticConfig config, KeeneticApi keeneticApi, DomainRoute route) : base(config)
    {
        _config = config;
        _keeneticApi = keeneticApi;
        _route = route;

        AliveKeepingStart(_cancellationTokenSource.Token);
    }

    public override int MaxAddressCount => KeeneticApi.MaxDomainRoutes;
    public override IReadOnlyCollection<string> Addresses => _route.Addresses.ToList();

    public override async Task SaveAsync(IReadOnlyCollection<string> addresses, CancellationToken cancellationToken)
    {
        _route.Addresses.Clear();
        _route.Addresses.AddRange(addresses);
        await _keeneticApi.SaveDomainRouteAsync(_route, cancellationToken);
    }

    public override async Task ShutdownAsync()
    {
        if (_config.ClearBeforeExit)
        {
            _route.Addresses.Clear();
            await _keeneticApi.SaveDomainRouteAsync(_route);
        }
    }

    private async void AliveKeepingStart(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
                await _keeneticApi.LoginAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }
    }

    public override void Dispose()
    {
        _cancellationTokenSource.Dispose();
        base.Dispose();
    }
}
