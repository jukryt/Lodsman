using Lodsman.Helper;
using Lodsman.Log;
using Lodsman.Model;

namespace Lodsman.Context.Router.Keenetic;

internal class KeeneticContext : BaseContext
{
    private readonly IKeeneticConfig _config;
    private readonly KeeneticApi _keeneticApi;
    private readonly DomainRoute[] _routes;
    private readonly int _maxAddressCount;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public KeeneticContext(IKeeneticConfig config, KeeneticApi keeneticApi, DomainRoute[] routes, ILog log) : base(config, log)
    {
        _config = config;
        _keeneticApi = keeneticApi;
        _routes = routes;
        _maxAddressCount = KeeneticApi.MaxDomainRoutes * routes.Length;

        AliveKeepingStart(_cancellationTokenSource.Token);
    }

    public override IReadOnlyCollection<string> Addresses => _routes.SelectMany(x => x.Addresses).ToList();

    public override IAddressCollection CreateAddressCollection() => new CidrIpSetCollection(_maxAddressCount, _config.AutoCollapse);

    public override async Task SaveAsync(IReadOnlyCollection<string> addresses, CancellationToken cancellationToken = default)
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

    public override async Task ShutdownAsync(IReadOnlyCollection<string> addresses)
    {
        if (_config.ClearBeforeExit)
            await SaveAsync([]);
        else
            await SaveAsync(addresses);
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
