using System.Net;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Session;

namespace Lodsman.Network.MicrosoftTraceEvent;

internal class TraceEventListener : INetworkListener
{
    public static TraceEventListener Start()
    {
        var listener = new TraceEventListener();
        listener.SessionStart();
        return listener;
    }

    private readonly TraceEventSession _tcpIpSession;

    private TraceEventListener()
    {
        _tcpIpSession = new TraceEventSession($"{nameof(Lodsman)}TcpIpSession");
        _tcpIpSession.EnableKernelProvider(KernelTraceEventParser.Keywords.NetworkTCPIP);
        _tcpIpSession.Source.Kernel.TcpIpSend += TcpIpv4Send;
        _tcpIpSession.Source.Kernel.TcpIpSendIPV6 += TcpIpv6Send;
        _tcpIpSession.Source.Kernel.UdpIpSend += UdpIpv4Send;
        _tcpIpSession.Source.Kernel.UdpIpSendIPV6 += UdpIpv6Send;
    }

    public event EventHandler<IpSendEventArgs>? IpSend;

    private void SessionStart()
    {
        Task.Run(_tcpIpSession.Source.Process);
    }

    private void TcpIpv4Send(TcpIpSendTraceData data)
    {
        OnIpSend(data.ProcessName, data.daddr);
    }

    private void TcpIpv6Send(TcpIpV6SendTraceData data)
    {
        OnIpSend(data.ProcessName, data.daddr);
    }

    private void UdpIpv4Send(UdpIpTraceData data)
    {
        OnIpSend(data.ProcessName, data.daddr);
    }

    private void UdpIpv6Send(UpdIpV6TraceData data)
    {
        OnIpSend(data.ProcessName, data.daddr);
    }

    protected virtual void OnIpSend(string processName, IPAddress targetIp)
    {
        IpSend?.Invoke(this, new IpSendEventArgs(processName, targetIp));
    }

    public void Dispose()
    {
        IpSend = null;
        _tcpIpSession.Dispose();
    }
}
