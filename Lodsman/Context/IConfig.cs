using Lodsman.Log;

namespace Lodsman.Context;

internal interface IConfig
{
    bool IsService { get; }
    string ServiceName { get; }
    List<string> ProcessNames { get; }
    uint SavingDelay { get; }
    bool ClearBeforeExit { get; }

    public Task<IContext> BuildContextAsync(ILog log, CancellationToken cancellationToken);
}
