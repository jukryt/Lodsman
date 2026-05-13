namespace Lodsman.Shutdown;

internal interface IShutdownAction
{
    Task ShutdownAsync();
}
