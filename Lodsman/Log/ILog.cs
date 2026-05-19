namespace Lodsman.Log;

internal interface ILog : IAsyncDisposable
{
    void Info(string message);
    void Warn(string message);
    void Error(string message);
    void Error(Exception exception);
}
