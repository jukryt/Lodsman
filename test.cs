#:package IPAddressRange@6.3.0

using System.Buffers.Binary;
using System.Collections;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using NetTools;

var addresses = new string[10000];

var ts = new Stopwatch();
var r = new Random();
for (var i = 0; i < addresses.Length; i++)
{
    var buffer = new byte[4];
    buffer[0] = 192;
    buffer[1] = 168;
    r.NextBytes(buffer);
    addresses[i] = new IPAddress(buffer).ToString();
}

Console.WriteLine($"Init Start");
var collection = new CidrIpSetCollection(autoCollapse: true);
ts.Restart();
collection.Init(addresses);
ts.Stop();
Console.WriteLine($"Init - {ts.Elapsed}");

var avgTryAdd = new List<TimeSpan>();
for (var i = 0; i < 10000; i++)
{
    var buffer = new byte[4];
    r.NextBytes(buffer);
    buffer[0] = 192;
    buffer[1] = 168;
    ts.Restart();
    collection.TryAdd(new IPAddress(buffer));
    ts.Stop();
    avgTryAdd.Add(ts.Elapsed);
}

Console.WriteLine($"TryAdd - {new TimeSpan(avgTryAdd.Sum(t => t.Ticks) / avgTryAdd.Count)}");

ts.Restart();
var t1 = collection.GetAll();
ts.Stop();
Console.WriteLine($"GetAll - {ts.Elapsed}");

ts.Restart();
var t2 = collection.GetOrdered();
ts.Stop();
Console.WriteLine($"GetOrdered - {ts.Elapsed}");
Console.WriteLine(collection.Count);

foreach (var t in t2.Order())
    Console.WriteLine(t);

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

    public CidrIpSet GetPreviousSet() => GetIpSet(MergeId - 1, IpVersion, Prefix);
    public CidrIpSet GetNextSet() => GetIpSet(MergeId + 1, IpVersion, Prefix);
    public bool Contains(IPAddress ipAddress) => _range.Contains(ipAddress);
    public bool Contains(CidrIpSet ipSet) => _range.Contains(ipSet._range);
    public override int GetHashCode() => _range.GetHashCode();
    public override bool Equals(object? obj) => _range.Equals(obj);
    public override string ToString() => Address;

    private static CidrIpSet GetIpSet(UInt128 mergeId, IpVersion ipVersion, int prefix)
    {
        byte[] bytes;
        switch (ipVersion)
        {
            case IpVersion.IPv4:
                bytes = new byte[4];
                BinaryPrimitives.WriteUInt32BigEndian(bytes, (uint)(mergeId << 1) * ((uint)1 << (32 - prefix)));
                break;
            case IpVersion.IPv6:
                bytes = new byte[16];
                BinaryPrimitives.WriteUInt128BigEndian(bytes, (mergeId << 1) * ((UInt128)1 << (32 - prefix)));
                break;
            default:
                throw new NotSupportedException(ipVersion.ToString());
        }

        var begin = new IPAddress(bytes);
        return new CidrIpSet(new IPAddressRange(begin, prefix));
    }
}

internal class CidrIpSetComparer : IComparer<CidrIpSet>, IEqualityComparer<CidrIpSet>
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

        return x.Prefix.CompareTo(y.Prefix);
    }

    public bool Equals(CidrIpSet? x, CidrIpSet? y)
    {
        return x switch
        {
            null when y == null => true,
            not null when y == null => false,
            null => false,
            _ => x.Equals(y)
        };
    }

    public int GetHashCode(CidrIpSet obj)
    {
        return obj.GetHashCode();
    }
}

internal class LinkedHashSet<T>(IEqualityComparer<T>? comparer = null) : IEnumerable<T> where T : notnull
{
    private readonly Dictionary<T, LinkedListNode<T>> _dictionary = new(comparer);
    private readonly LinkedList<T> _linkedList = [];

    public int Count => _linkedList.Count;

    public bool Add(T item)
    {
        if (_dictionary.ContainsKey(item))
            return false;

        var node = _linkedList.AddLast(item);
        _dictionary[item] = node;
        return true;
    }

    public bool Remove(T item)
    {
        if (!_dictionary.Remove(item, out var node))
            return false;

        _linkedList.Remove(node);
        return true;
    }

    public bool Replace(T oldItem, T newItem)
    {
        if (_dictionary.ContainsKey(newItem) ||
            !_dictionary.Remove(oldItem, out var node))
            return false;

        node.Value = newItem;
        _dictionary[newItem] = node;
        return true;
    }

    public void Clear()
    {
        _dictionary.Clear();
        _linkedList.Clear();
    }

    public IEnumerator<T> GetEnumerator() => _linkedList.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

internal class CidrIpSetCollection(int maxCount = int.MaxValue, bool autoCollapse = false) : BaseAddressCollection
{
    private readonly SortedList<CidrIpSet, long> _ipSets = new(new CidrIpSetComparer());
    private readonly LinkedHashSet<string> _addresses = new(StringComparer.Ordinal);

    public override int Count => _addresses.Count;
    public override int MaxCount => maxCount;

    public override bool Init(IEnumerable<string> addresses)
    {
        _ipSets.Clear();
        _addresses.Clear();

        foreach (var address in addresses)
        {
            if (CidrIpSet.TryParse(address, out var ipSet) &&
                _ipSets.TryAdd(ipSet, Stopwatch.GetTimestamp()))
            {
                _addresses.Add(ipSet.Address);
                OnAddressLoad(address);
            }
            else
                _addresses.Add(address);
        }

        var isFiltered = Filter();
        var isCollapsed = autoCollapse && Collapse();

        return isFiltered || isCollapsed;
    }

    public override bool TryAdd(IPAddress ipAddress)
    {
        foreach (var ipSet in _ipSets.Keys)
        {
            if (!ipSet.Contains(ipAddress))
                continue;

            _ipSets[ipSet] = Stopwatch.GetTimestamp();
            return false;
        }

        var newIpSet = new CidrIpSet(ipAddress);
        if (!_ipSets.TryAdd(newIpSet, Stopwatch.GetTimestamp()))
            return false;

        _addresses.Add(newIpSet.Address);

        OnAddressAdded(ipAddress.ToString());

        if (autoCollapse)
            Collapse();

        while (_addresses.Count > maxCount)
        {
            var oldIpSet = _ipSets.MinBy(x => x.Value).Key;
            _ipSets.Remove(oldIpSet, out _);
            _addresses.Remove(oldIpSet.Address);
            OnAddressRemove(oldIpSet.Address);
        }

        return true;
    }

    public override IReadOnlyCollection<string> GetAll()
    {
        return _addresses.ToList();
    }

    public override IReadOnlyCollection<string> GetOrdered()
    {
        var addresses = _ipSets
            .OrderBy(x => x.Value)
            .Select(x => x.Key.Address)
            .ToList();

        return _addresses.Except(addresses)
            .Union(addresses)
            .ToList();
    }

    private bool Filter()
    {
        var isChanged = false;

        for (var i = 0; i < _ipSets.Count; i++)
        {
            var current = _ipSets.Keys[i];
            for (var j = 0; j < _ipSets.Count; j++)
            {
                if (i == j)
                    continue;

                var challenger = _ipSets.Keys[j];
                if (!current.Contains(challenger))
                    continue;

                _ipSets.Remove(challenger);
                _addresses.Remove(challenger.Address);
                isChanged = true;

                if (j < i)
                    i--;

                j--;
            }
        }

        return isChanged;
    }

    private bool Collapse()
    {
        var isChanged = false;

        for (var i = 1; i < _ipSets.Count; i++)
        {
            var current = _ipSets.Keys[i - 1];
            var next = _ipSets.Keys[i];

            if (current.IpVersion != next.IpVersion)
                continue;

            if (current.Prefix != next.Prefix)
                continue;

            if (current.MergeId != next.MergeId)
                continue;

            var merged = new CidrIpSet(current.Begin, next.End);

            _ipSets.Remove(current);
            _ipSets.Remove(next);
            _addresses.Remove(next.Address);

            if (_ipSets.ContainsKey(merged))
            {
                _ipSets[merged] = Stopwatch.GetTimestamp();
                _addresses.Remove(current.Address);
            }
            else
            {
                _ipSets.Add(merged, Stopwatch.GetTimestamp());
                _addresses.Replace(current.Address, merged.Address);
            }

            isChanged = true;
            i -= Math.Min(i, 2);

            OnAddressMerge([current.Address, next.Address], merged.Address);
        }

        return isChanged;
    }
}

internal abstract class BaseAddressCollection : IAddressCollection
{
    public abstract int Count { get; }
    public abstract int MaxCount { get; }

    public event EventHandler<AddressEventArgs>? AddressLoad;
    public event EventHandler<AddressEventArgs>? AddressAdded;
    public event EventHandler<AddressEventArgs>? AddressRemove;
    public event EventHandler<MergeEventArgs>? AddressMerge;

    public abstract bool Init(IEnumerable<string> addresses);
    public abstract bool TryAdd(IPAddress ipAddress);
    public abstract IReadOnlyCollection<string> GetAll();
    public abstract IReadOnlyCollection<string> GetOrdered();

    protected void OnAddressLoad(string address) => AddressLoad?.Invoke(this, new AddressEventArgs(address));
    protected void OnAddressAdded(string address) => AddressAdded?.Invoke(this, new AddressEventArgs(address));
    protected void OnAddressRemove(string address) => AddressRemove?.Invoke(this, new AddressEventArgs(address));
    protected void OnAddressMerge(string[] source, string target) => AddressMerge?.Invoke(this, new MergeEventArgs(source, target));
}

internal interface IAddressCollection
{
    int Count { get; }
    int MaxCount { get; }

    event EventHandler<AddressEventArgs>? AddressLoad;
    event EventHandler<AddressEventArgs>? AddressAdded;
    event EventHandler<AddressEventArgs>? AddressRemove;
    event EventHandler<MergeEventArgs>? AddressMerge;

    bool Init(IEnumerable<string> addresses);
    bool TryAdd(IPAddress ipAddress);
    IReadOnlyCollection<string> GetAll();
    IReadOnlyCollection<string> GetOrdered();
}

internal class AddressEventArgs(string address) : EventArgs
{
    public string Address { get; } = address;
}

internal class MergeEventArgs(string[] source, string target) : EventArgs
{
    public string[] SourceAddresses { get; } = source;
    public string TargetAddress { get; } = target;
}

internal enum IpVersion
{
    IPv4,
    IPv6,
}
