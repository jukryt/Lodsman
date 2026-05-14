using Lodsman.AddressSaver;
using Lodsman.Log;
using Lodsman.Shutdown;

namespace Lodsman.Context;

internal interface IContext : IDisposable
{
    string ServiceName { get; }
    ILog Log { get; }
    IReadOnlyCollection<string> ProcessNames { get; }
    IReadOnlyCollection<string> Addresses { get; }
    IAddressSaverAction AddressSaverAction { get; }
    IShutdownAction ShutdownAction { get; }
}
