using System.Net;

namespace Lodsman.Network;

internal record ConnectionInfo
{
    public required IPAddress SourceIp { get; init; }
    public required int SourcePort { get; init; }
    public required IPAddress TargetIp { get; init; }
    public required int TargetPort { get; init; }
}
