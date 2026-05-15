using Lodsman.AddressSaver;
using Lodsman.Log;
using Microsoft.Extensions.Hosting.WindowsServices;

namespace Lodsman.Context;

internal abstract class BaseContext : IContext
{
    private readonly IConfig _config;

    protected BaseContext(IConfig config)
    {
        _config = config;
        Log = CreateLog();
    }

    public string ServiceName => $"{App.Name} - {string.Join(", ", ProcessNames.Order().ToHashSet(StringComparer.OrdinalIgnoreCase))}";
    public IReadOnlyCollection<string> ProcessNames => _config.ProcessNames;
    public abstract IReadOnlyCollection<string> Addresses { get; }
    public abstract IAddressSaverAction AddressSaverAction { get; }
    public ILog Log { get; }

    public abstract Task ShutdownAsync();

    private ILog CreateLog()
    {
        if (WindowsServiceHelpers.IsWindowsService())
        {
            var logFileName = $"{string.Join("_", ServiceName.Split(Path.GetInvalidFileNameChars()))}.log";
            var logFilePath = Path.Combine(FileSystemHelper.GetAppDataFolder(), logFileName);
            return new FileLog(logFilePath);
        }

        return new ConsoleLog();
    }

    public virtual void Dispose()
    {
    }
}
