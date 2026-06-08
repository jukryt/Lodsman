using System.Text.Json.Serialization;
using DotMake.CommandLine;
using Lodsman.Helper;
using Lodsman.Log;
using Microsoft.Extensions.Hosting.WindowsServices;

namespace Lodsman.CliRunner
{
    internal abstract class BaseCommand : RootCommand, ICliRunAsyncWithReturn
    {
        [JsonIgnore]
        public bool IsService => WindowsServiceHelpers.IsWindowsService();

        public abstract string Name { get; set; }

        [CliOption(Alias = "-is", Required = false, Arity = CliArgumentArity.ZeroOrOne, Group = "install")]
        [JsonIgnore]
        public bool InstallService { get; set; } = false;

        [CliOption(Alias = "-us", Required = false, Arity = CliArgumentArity.ZeroOrOne, Group = "install")]
        [JsonIgnore]
        public bool UninstallService { get; set; } = false;

        public async Task<int> RunAsync()
        {
            await using var log = CreateLog();

            try
            {
                await InitAsync(log);

                if (IsService)
                    await RunAsServiceAsync(log);
                else if (InstallService)
                    await InstallServiceAsync(log);
                else if (UninstallService)
                    await UninstallServiceAsync(log);
                else
                    await RunAsConsoleAsync(log);
            }
            catch (OperationCanceledException)
            {
                // ignore
            }
            catch (Exception ex)
            {
                log.Error(ex);
                if (IsService)
                    Environment.Exit(ex.HResult);
                return ex.HResult;
            }

            return 0;
        }

        public abstract Task InitAsync(ILog log);
        public abstract Task RunAsServiceAsync(ILog log);
        public abstract Task RunAsConsoleAsync(ILog log);

        protected abstract IEnumerable<string> GetServiceArguments();

        private async Task InstallServiceAsync(ILog log)
        {
            var servicePath = Environment.ProcessPath;
            if (!File.Exists(servicePath))
                throw new Exception($"Service not found in path: {servicePath}");

            var serviceArguments = GetServiceArguments().Select(x => x.Replace("\"", "\\\"")).ToList();
            var installArguments = $"/c sc create \"{Name}\" binPath= \"\\\"{servicePath}\\\" {string.Join(" ", serviceArguments)}\" start= auto";
            log.Info($"Install Service: \"{Name}\"");
            var installResult = await ProcessHelper.ExecuteAsync("cmd", installArguments, log);
            log.Info($"Result code: {installResult}");
            if (installResult != 0 && installResult != 1073)
                return;

            var startArguments = $"/c sc start \"{Name}\"";
            log.Info($"Start Service: \"{Name}\"");
            var startResult = await ProcessHelper.ExecuteAsync("cmd", startArguments, log);
            log.Info($"Result code: {startResult}");
        }

        private async Task UninstallServiceAsync(ILog log)
        {
            var stopArguments = $"/c sc stop \"{Name}\"";
            log.Info($"Stop Service: \"{Name}\"");
            var stopResult = await ProcessHelper.ExecuteAsync("cmd", stopArguments, log);
            log.Info($"Result code: {stopResult}");

            var deleteArguments = $"/c sc delete \"{Name}\"";
            log.Info($"Uninstall Service: \"{Name}\"");
            var deleteResult = await ProcessHelper.ExecuteAsync("cmd", deleteArguments, log);
            log.Info($"Result code: {deleteResult}");
        }

        private ILog CreateLog()
        {
            if (IsService)
            {
                var logFileName = FileSystemHelper.NormalizeFileName($"{Name}.log");
                var logFilePath = Path.Combine(FileSystemHelper.GetAppDataFolder(), logFileName);
                return new FileLog(logFilePath);
            }

            return new ConsoleLog();
        }
    }
}
