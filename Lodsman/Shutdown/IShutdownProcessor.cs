namespace Lodsman.Shutdown;

internal interface IShutdownProcessor : IDisposable
{
    CancellationToken CancellationToken { get; }
    Task EndWorkAwaiter { get; }
    Task<int> ExitAppAwaiter { get; }
}
