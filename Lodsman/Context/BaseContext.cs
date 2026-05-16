using Lodsman.Log;
using Lodsman.Main;
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
    public abstract int MaxAddressCount { get; }
    public IReadOnlyCollection<string> ProcessNames => _config.ProcessNames;
    public abstract IReadOnlyCollection<string> Addresses { get; }
    public ILog Log { get; }

    public abstract Task SaveAsync(IReadOnlyCollection<string> addresses, CancellationToken cancellationToken);
    public abstract Task ShutdownAsync();

    private ILog CreateLog()
    {
        if (_config.IsService)
        {
            var logFileName = FileSystemHelper.NormalizeFileName($"{ServiceName}.log");
            var logFilePath = Path.Combine(FileSystemHelper.GetAppDataFolder(), logFileName);
            return new FileLog(logFilePath);
        }

        return new ConsoleLog();
    }

    public virtual async ValueTask DisposeAsync()
    {
        await Log.DisposeAsync();
    }
}
