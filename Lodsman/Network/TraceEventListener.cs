using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Session;

namespace Lodsman.Network;

internal class TraceEventListener : IDisposable
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
        var info = new ConnectionInfo
        {
            SourceIp = data.saddr,
            SourcePort = data.sport,
            TargetIp = data.daddr,
            TargetPort = data.dport,
        };

        OnConnection(data.ProcessID, data.ProcessName, info);
    }

    private void TcpIpv6Send(TcpIpV6SendTraceData data)
    {
        var info = new ConnectionInfo
        {
            SourceIp = data.saddr,
            SourcePort = data.sport,
            TargetIp = data.daddr,
            TargetPort = data.dport,
        };

        OnConnection(data.ProcessID, data.ProcessName, info);
    }

    private void UdpIpv4Send(UdpIpTraceData data)
    {
        var info = new ConnectionInfo
        {
            SourceIp = data.saddr,
            SourcePort = data.sport,
            TargetIp = data.daddr,
            TargetPort = data.dport,
        };

        OnConnection(data.ProcessID, data.ProcessName, info);
    }

    private void UdpIpv6Send(UpdIpV6TraceData data)
    {
        var info = new ConnectionInfo
        {
            SourceIp = data.saddr,
            SourcePort = data.sport,
            TargetIp = data.daddr,
            TargetPort = data.dport,
        };

        OnConnection(data.ProcessID, data.ProcessName, info);
    }

    public void Dispose()
    {
        Connection = null;
        _session.Dispose();
    }

    protected virtual void OnConnection(int processId, string processName, ConnectionInfo info)
    {
        Connection?.Invoke(this, new ConnectionEventArgs(processId, processName, info));
    }
}

internal class ConnectionEventArgs(int processId, string processName, ConnectionInfo info) : EventArgs
{
    public int ProcessId { get; } = processId;
    public string ProcessName { get; } = processName;
    public ConnectionInfo Info { get; } = info;
}
