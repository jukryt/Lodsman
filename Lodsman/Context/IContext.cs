using Lodsman.Log;

namespace Lodsman.Context;

internal interface IContext : IAsyncDisposable
{
    int MaxAddressCount { get; }
    IReadOnlyCollection<string> ProcessNames { get; }
    IReadOnlyCollection<string> Addresses { get; }
    TimeSpan SavingDelay { get; }
    ILog Log { get; }

    Task SaveAsync(IReadOnlyCollection<string> addresses, CancellationToken cancellationToken);
    Task ShutdownAsync();
}
