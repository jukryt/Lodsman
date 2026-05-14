using Lodsman.AddressSaver;
using Lodsman.Log;
using Lodsman.Shutdown;

namespace Lodsman.Router.Keenetic;

internal class KeeneticContext : IContext, IAddressSaverAction, IShutdownAction
{
    public static async Task<KeeneticContext> BuildAsync(IConfig config, ILog log)
    {
        var keeneticApi = new KeeneticApi(HttpClientHelper.Instance, config.KeenAddress, config.KeenUser, config.KeenPassword);
        var route = await keeneticApi.GetDomainRouteAsync(config.KeenListName);

        return new KeeneticContext(config, keeneticApi, route, log);
    }

    private readonly IConfig _config;
    private readonly ILog _log;
    private readonly KeeneticApi _keeneticApi;
    private readonly DomainRoute _route;
    private readonly CancellationTokenSource _cancellationTokenSource = new ();

    private KeeneticContext(IConfig config, KeeneticApi keeneticApi, DomainRoute route, ILog log)
    {
        _config = config;
        _keeneticApi = keeneticApi;
        _route = route;
        _log = log;

        AliveKeepingStart(_cancellationTokenSource.Token);
    }

    public int MaxAddressCount => KeeneticApi.MaxDomainRoutes;
    public IReadOnlyCollection<string> ProcessNames => _config.ProcessNames;
    public IReadOnlyCollection<string> Addresses => _route.Addresses.ToList();
    public IAddressSaverAction AddressSaverAction => this;
    public IShutdownAction ShutdownAction => this;

    private async Task SaveAsync(IReadOnlyCollection<string> addresses, CancellationToken cancellationToken)
    {
        _route.Addresses.Clear();
        _route.Addresses.AddRange(addresses);
        await _keeneticApi.SaveDomainRouteAsync(_route, cancellationToken);
    }

    private async Task ShutdownAsync()
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
            catch (Exception ex)
            {
                _log.Error(ex);
            }
        }
    }

    Task IAddressSaverAction.SaveAsync(IReadOnlyCollection<string> addresses, CancellationToken cancellationToken) => SaveAsync(addresses, cancellationToken);
    Task IShutdownAction.ShutdownAsync() => ShutdownAsync();

    public void Dispose()
    {
        _cancellationTokenSource.Dispose();
    }
}
