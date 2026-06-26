using Lodsman.Log;
using Lodsman.Model;

namespace Lodsman.Context;

internal abstract class BaseContext(IConfig config, ILog log) : IContext
{
    public IReadOnlyCollection<string> ProcessNames => config.ProcessNames;
    public abstract IReadOnlyCollection<string> Addresses { get; }
    public TimeSpan SavingDelay => TimeSpan.FromMilliseconds(config.SavingDelay);
    public bool ShowAddressesOnLoad => config.ShowAddressesOnLoad;
    public ILog Log { get; } = log;

    public abstract IAddressCollection CreateAddressCollection();
    public abstract Task SaveAsync(IReadOnlyCollection<string> addresses, CancellationToken cancellationToken = default);
    public abstract Task ShutdownAsync(IReadOnlyCollection<string> addresses);

    public virtual ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
