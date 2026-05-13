namespace Lodsman.Log;

internal interface ILog
{
    void Info(string message);
    void Warn(string message);
    void Error(string message);
    void Error(Exception exception);
}
