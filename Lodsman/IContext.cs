using Lodsman.AddressSaver;
using Lodsman.Shutdown;

namespace Lodsman;

internal interface IContext : IDisposable
{
    IReadOnlyCollection<string> ProcessNames { get; }
    IReadOnlyCollection<string> Addresses { get; }
    IAddressSaverAction AddressSaverAction { get; }
    IShutdownAction ShutdownAction { get; }
}
