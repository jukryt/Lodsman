using Lodsman.Log;

namespace Lodsman.Context;

internal abstract class BaseContext(IConfig config, ILog log) : IContext
{
    public abstract int MaxAddressCount { get; }
    public IReadOnlyCollection<string> ProcessNames => config.ProcessNames;
    public abstract IReadOnlyCollection<string> Addresses { get; }
    public TimeSpan SavingDelay => TimeSpan.FromMilliseconds(config.SavingDelay);
    public ILog Log { get; } = log;

    public abstract Task SaveAsync(IReadOnlyCollection<string> addresses, CancellationToken cancellationToken);

    public abstract Task ShutdownAsync();

    public virtual ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
