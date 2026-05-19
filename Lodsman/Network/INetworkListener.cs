namespace Lodsman.Network;

internal interface INetworkListener : IDisposable
{
    event EventHandler<ConnectionEventArgs>? Connection;
}
