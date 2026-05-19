using Lodsman.Context;
using Lodsman.Log;

namespace Lodsman.Main;

internal class AppExecutor(IConfig config, ILog log)
{
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        //System.Diagnostics.Debugger.Launch();

        log.Info("Init...");
        await using var context = await config.BuildContextAsync(log, cancellationToken);

        var app = new App(context);
        log.Info("Running...");
        await app.ExecuteAsync(cancellationToken);
        log.Info("Shutdown...");
        await app.ShutdownAsync();
    }
}
