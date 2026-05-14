using Lodsman.AddressSaver;
using Lodsman.Log;
using Lodsman.Shutdown;
using Microsoft.Extensions.Hosting;

namespace Lodsman;

internal class ServiceWorker(IConfig config, string serviceName) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var programDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        var logFileName = $"{string.Join("_", serviceName.Split(Path.GetInvalidFileNameChars()))}.log";
        var logFilePath = Path.Combine(programDataFolder, App.Name, logFileName);
        var log = new FileLog(logFilePath);

        try
        {
            using var context = await ContextBuilder.BuildAsync(config, log);
            using var shutdownProcessor = new CancellationTokenShutdownProcessor(context.ShutdownAction, log, stoppingToken);
            var addressSaverProcessor = new AddressSaverProcessor(context.AddressSaverAction, log, shutdownProcessor.CancellationToken);
            var app = new App(context, addressSaverProcessor, shutdownProcessor, log);

            await app.RunAsync();
        }
        catch (Exception ex)
        {
            log.Error(ex);
            Environment.Exit(ex.HResult);
        }
    }
}
