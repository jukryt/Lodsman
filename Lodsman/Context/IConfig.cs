using Lodsman.Log;

namespace Lodsman.Context;

internal interface IConfig
{
    string Name { get; }
    List<string> ProcessNames { get; }
    uint SavingDelay { get; }
    bool ShowAddressesOnLoad { get; }
    bool ClearBeforeExit { get; }

    public Task<IContext> BuildContextAsync(ILog log, CancellationToken cancellationToken);
}
