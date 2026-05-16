using Lodsman.Log;

namespace Lodsman.Context;

internal interface IContext : IAsyncDisposable
{
    string ServiceName { get; }
    int MaxAddressCount { get; }
    IReadOnlyCollection<string> ProcessNames { get; }
    IReadOnlyCollection<string> Addresses { get; }
    ILog Log { get; }

    Task SaveAsync(IReadOnlyCollection<string> addresses, CancellationToken cancellationToken);
    Task ShutdownAsync();
}
