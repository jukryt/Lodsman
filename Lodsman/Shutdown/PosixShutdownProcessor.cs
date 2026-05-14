using System.Runtime.InteropServices;
using Lodsman.Log;

namespace Lodsman.Shutdown;

internal class PosixShutdownProcessor : BaseShutdownProcessor
{
    private readonly PosixSignalRegistration[] _signalRegistration;

    public PosixShutdownProcessor(IShutdownAction action, ILog log) : base(action, log)
    {
        _signalRegistration =
        [
            PosixSignalRegistration.Create(PosixSignal.SIGINT, ShutdownSignal),
            PosixSignalRegistration.Create(PosixSignal.SIGQUIT, ShutdownSignal),
            PosixSignalRegistration.Create(PosixSignal.SIGHUP, ShutdownSignal),
            PosixSignalRegistration.Create(PosixSignal.SIGTERM, ShutdownSignal),
        ];
    }

    private void ShutdownSignal(PosixSignalContext context)
    {
        context.Cancel = true;
        Shutdown();
    }

    public override void Dispose()
    {
        foreach (var signalRegistration in _signalRegistration)
            signalRegistration.Dispose();

        base.Dispose();
    }
}
