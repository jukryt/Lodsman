using Lodsman.AddressSaver;
using Lodsman.Log;

namespace Lodsman.Context;

internal interface IContext : IDisposable
{
    string ServiceName { get; }
    IReadOnlyCollection<string> ProcessNames { get; }
    IReadOnlyCollection<string> Addresses { get; }
    IAddressSaverAction AddressSaverAction { get; }
    ILog Log { get; }

    Task ShutdownAsync();
}
