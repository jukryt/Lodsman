using System.Diagnostics;
using Lodsman.Log;

namespace Lodsman;

internal class ProcessHelper
{
    public static Task<int> ExecuteAsync(string filePath, string arguments, ILog log, CancellationToken cancellationToken = default)
    {
        return ExecuteAsync(new ProcessStartInfo(filePath) { Arguments = arguments }, log, cancellationToken);
    }

    public static async Task<int> ExecuteAsync(ProcessStartInfo startInfo, ILog log, CancellationToken cancellationToken = default)
    {
        var processCompletionSource = new TaskCompletionSource<int>();
        if (cancellationToken != default)
            cancellationToken.Register(processCompletionSource.SetCanceled);

        startInfo.CreateNoWindow = true;
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardOutput = true;

        var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        process.OutputDataReceived += (_, e) => log.Info(e.Data ?? string.Empty);
        process.Exited += (_, _) => processCompletionSource.TrySetResult(process.ExitCode);

        process.Start();
        var result = await processCompletionSource.Task;

        return result;
    }
}
