using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using NetTools;

namespace Lodsman.Model;

internal class CidrIpSet
{
    private static readonly CidrIpSet EmptyRange = new(new IPAddressRange());

    public static bool TryParse(string address, out CidrIpSet ipSet)
    {
        if (!IPAddressRange.TryParse(address, out var ipRange))
        {
            ipSet = EmptyRange;
            return false;
        }

        ipSet = new CidrIpSet(ipRange);
        return true;
    }

    private readonly IPAddressRange _range;

    public CidrIpSet(IPAddress single) : this(new IPAddressRange(single))
    {
    }

    public CidrIpSet(IPAddress begin, IPAddress end) : this(new IPAddressRange(begin, end))
    {
    }

    private CidrIpSet(IPAddressRange range)
    {
        _range = range;
        Address = range.AddressCount > 1
            ? range.ToCidrString()
            : range.Begin.ToString();
        IpVersion = range.Begin.AddressFamily switch
        {
            AddressFamily.InterNetwork => IpVersion.IPv4,
            AddressFamily.InterNetworkV6 => IpVersion.IPv6,
            _ => throw new NotSupportedException(range.Begin.AddressFamily.ToString())
        };
        Prefix = range.GetPrefixLength();
        MergeId = IpVersion switch
        {
            IpVersion.IPv4 => (BinaryPrimitives.ReadUInt32BigEndian(range.Begin.GetAddressBytes()) / ((UInt128)1 << (32 - Prefix))) >> 1,
            IpVersion.IPv6 => (BinaryPrimitives.ReadUInt128BigEndian(range.Begin.GetAddressBytes()) / ((UInt128)1 << (128 - Prefix))) >> 1,
            _ => throw new NotSupportedException(IpVersion.ToString())
        };
    }

    public string Address { get; }
    public AddressFamily AddressFamily => _range.Begin.AddressFamily;
    public IPAddress Begin => _range.Begin;
    public IPAddress End => _range.End;
    public IpVersion IpVersion { get; }
    public int Prefix { get; }
    public UInt128 MergeId { get; }

    public bool Contains(IPAddress ipAddress) => _range.Contains(ipAddress);
    public override int GetHashCode() => _range.GetHashCode();
    public override bool Equals(object? obj) => _range.Equals(obj);
}

internal class CidrIpSetComparer : IComparer<CidrIpSet>
{
    public int Compare(CidrIpSet? x, CidrIpSet? y)
    {
        switch (x)
        {
            case null when y == null:
                return 0;
            case not null when y == null:
                return 1;
            case null:
                return -1;
        }

        if (x.AddressFamily != y.AddressFamily)
            return x.AddressFamily.CompareTo(y.AddressFamily);

        var xBytes = x.Begin.GetAddressBytes();
        var yBytes = y.Begin.GetAddressBytes();

        for (var i = 0; i < xBytes.Length; i++)
        {
            if (xBytes[i] != yBytes[i])
                return xBytes[i].CompareTo(yBytes[i]);
        }

        return 0;
    }
}