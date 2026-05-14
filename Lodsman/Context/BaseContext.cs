using Lodsman.AddressSaver;
using Lodsman.Log;
using Lodsman.Shutdown;
using Microsoft.Extensions.Hosting.WindowsServices;

namespace Lodsman.Context
{
    internal abstract class BaseContext : IContext
    {
        private readonly IConfig _config;

        protected BaseContext(IConfig config)
        {
            _config = config;
            Log = CreateLog();
        }

        public string ServiceName => $"{App.Name} - {string.Join(", ", ProcessNames.Order().ToHashSet(StringComparer.OrdinalIgnoreCase))}";
        public ILog Log { get; }
        public IReadOnlyCollection<string> ProcessNames => _config.ProcessNames;
        public abstract IReadOnlyCollection<string> Addresses { get; }
        public abstract IAddressSaverAction AddressSaverAction { get; }
        public abstract IShutdownAction ShutdownAction { get; }

        private ILog CreateLog()
        {
            if (WindowsServiceHelpers.IsWindowsService())
            {
                var programDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                var logFileName = $"{string.Join("_", ServiceName.Split(Path.GetInvalidFileNameChars()))}.log";
                var logFilePath = Path.Combine(programDataFolder, App.Name, logFileName);
                return new FileLog(logFilePath);
            }

            return new ConsoleLog();
        }

        public virtual void Dispose()
        {
        }
    }
}
