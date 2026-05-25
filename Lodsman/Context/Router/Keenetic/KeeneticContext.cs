using Lodsman.Log;

namespace Lodsman.Context.Router.Keenetic;

internal class KeeneticContext : BaseContext
{
    private readonly IKeeneticConfig _config;
    private readonly KeeneticApi _keeneticApi;
    private readonly DomainRoute[] _routes;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public KeeneticContext(IKeeneticConfig config, KeeneticApi keeneticApi, DomainRoute[] routes, ILog log) : base(config, log)
    {
        _config = config;
        _keeneticApi = keeneticApi;
        _routes = routes;

        AliveKeepingStart(_cancellationTokenSource.Token);
    }

    public override int MaxAddressCount => KeeneticApi.MaxDomainRoutes * _routes.Length;
    public override IReadOnlyCollection<string> Addresses => _routes.SelectMany(x => x.Addresses).ToList();

    public override async Task SaveAsync(IReadOnlyCollection<string> addresses, CancellationToken cancellationToken)
    {
        var itemIndex = 0;
        var addressesChunks = addresses
            .GroupBy(_ => itemIndex++ / KeeneticApi.MaxDomainRoutes)
            .Select(g => g.ToList())
            .ToList();

        for (var i = 0; i < _routes.Length; i++)
        {
            var route = _routes[i];
            if (addressesChunks.Count > i)
            {
                var addressesChunk = addressesChunks[i];
                var needSave = addressesChunk.Count != route.Addresses.Count;

                if (!needSave)
                    needSave = addressesChunk.Intersect(route.Addresses).Count() != addressesChunk.Count;

                if (!needSave)
                    continue;

                route.Addresses.Clear();
                route.Addresses.AddRange(addressesChunk);
                await _keeneticApi.SaveDomainRouteAsync(route, cancellationToken);
            }
            else if (route.Addresses.Any())
            {
                route.Addresses.Clear();
                await _keeneticApi.SaveDomainRouteAsync(route, cancellationToken);
            }
        }
    }

    public override async Task ShutdownAsync()
    {
        if (_config.ClearBeforeExit)
        {
            foreach (var domainRoute in _routes)
            {
                domainRoute.Addresses.Clear();
                await _keeneticApi.SaveDomainRouteAsync(domainRoute);
            }
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
            catch (HttpConnectException)
            {
                // ignore
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }
    }

    public override async ValueTask DisposeAsync()
    {
        _cancellationTokenSource.Dispose();
        await base.DisposeAsync();
    }
}
