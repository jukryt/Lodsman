using System.Net;

namespace Lodsman.Network;

internal class ConnectionEventArgs(string processName, IPAddress targetIp) : EventArgs
{
    public string ProcessName { get; } = processName;
    public IPAddress TargetIp { get; } = targetIp;
}
