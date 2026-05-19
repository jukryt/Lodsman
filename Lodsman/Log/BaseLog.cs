namespace Lodsman.Log;

internal abstract class BaseLog : ILog
{
    public void Info(string message)
    {
        Write(message);
    }

    public void Warn(string message)
    {
        Write($"warn: {message}");
    }

    public void Error(string message)
    {
        Write($"error: {message}");
    }

    public void Error(Exception exception)
    {
        var message = $"{exception.GetType().Name}: {exception.Message}\n{exception.StackTrace}";
        Error(message);
    }

    protected abstract void Write(string message);

    public virtual ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
