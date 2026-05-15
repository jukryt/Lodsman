using System.Net;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Session;

namespace Lodsman.Network;

internal class TraceEventListener : INetworkListener
{
    public static TraceEventListener Start()
    {
        var listener = new TraceEventListener();
        listener.SessionStart();
        return listener;
    }

    private readonly TraceEventSession _session;

    private TraceEventListener()
    {
        _session = new TraceEventSession(KernelTraceEventParser.KernelSessionName);
        _session.EnableKernelProvider(KernelTraceEventParser.Keywords.NetworkTCPIP);

        _session.Source.Kernel.TcpIpSend += TcpIpv4Send;
        _session.Source.Kernel.TcpIpSendIPV6 += TcpIpv6Send;
        _session.Source.Kernel.UdpIpSend += UdpIpv4Send;
        _session.Source.Kernel.UdpIpSendIPV6 += UdpIpv6Send;
    }

    public event EventHandler<ConnectionEventArgs>? Connection;

    private void SessionStart()
    {
        Task.Run(_session.Source.Process);
    }

    private void TcpIpv4Send(TcpIpSendTraceData data)
    {
        OnConnection(data.ProcessName, data.daddr);
    }

    private void TcpIpv6Send(TcpIpV6SendTraceData data)
    {
        OnConnection(data.ProcessName, data.daddr);
    }

    private void UdpIpv4Send(UdpIpTraceData data)
    {
        OnConnection(data.ProcessName, data.daddr);
    }

    private void UdpIpv6Send(UpdIpV6TraceData data)
    {
        OnConnection(data.ProcessName, data.daddr);
    }

    protected virtual void OnConnection(string processName, IPAddress targetIp)
    {
        Connection?.Invoke(this, new ConnectionEventArgs(processName, targetIp));
    }

    public void Dispose()
    {
        Connection = null;
        _session.Dispose();
    }
}
