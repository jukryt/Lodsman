using Lodsman.Log;
using Lodsman.Model;

namespace Lodsman.Context;

internal interface IContext : IAsyncDisposable
{
    IReadOnlyCollection<string> ProcessNames { get; }
    IReadOnlyCollection<string> Addresses { get; }
    TimeSpan SavingDelay { get; }
    bool ShowAddressesOnLoad { get; }
    ILog Log { get; }

    IAddressCollection CreateAddressCollection();
    Task SaveAsync(IReadOnlyCollection<string> addresses, CancellationToken cancellationToken = default);
    Task ShutdownAsync(IReadOnlyCollection<string> addresses);
}
