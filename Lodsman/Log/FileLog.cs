using System.Threading.Channels;
using Lodsman.Extension;

namespace Lodsman.Log;

internal class FileLog : BaseLog
{
    private readonly string _filePath;
    private readonly Channel<string> _channel;
    private readonly Task _writeTask;

    public FileLog(string filePath)
    {
        Create(filePath);
        _filePath = filePath;
        _channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions { SingleReader = true });
        _writeTask = ProcessLogAsync();
    }

    protected override void Write(string message)
    {
        if (!_writeTask.IsCompleted)
            _channel.Writer.TryWrite(message);
    }

    private async Task ProcessLogAsync()
    {
        var reader = _channel.Reader;

        await using var fileStream = new FileStream(_filePath, FileMode.Create, FileAccess.Write, FileShare.Read, 4096, useAsync: true);
        await using var streamWriter = new StreamWriter(fileStream);

        await foreach (var message in reader.ReadAllAsync())
        {
            await streamWriter.WriteLineAsync(message).IgnoreException();
            await streamWriter.FlushAsync().IgnoreException();
        }
    }

    private static void Create(string filePath)
    {
        try
        {
            var folderPath = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(folderPath))
                Directory.CreateDirectory(folderPath);
        }
        catch
        {
            // ignore
        }
    }

    public override async ValueTask DisposeAsync()
    {
        _channel.Writer.TryComplete();
        await _writeTask.IgnoreException();
        await base.DisposeAsync();
    }
}
