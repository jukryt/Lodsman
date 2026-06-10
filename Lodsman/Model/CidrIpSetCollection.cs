using System.Diagnostics;
using System.Net;

namespace Lodsman.Model;

internal class CidrIpSetCollection(int maxCount = int.MaxValue, bool autoCollapse = false) : BaseAddressCollection
{
    private readonly SortedList<CidrIpSet, long> _ipSets = new(new CidrIpSetComparer());
    private readonly HashSet<string> _others = new(StringComparer.Ordinal);

    public override int Count => _ipSets.Count + _others.Count;
    public override int MaxCount => maxCount;

    public override bool Init(IReadOnlyCollection<string> addresses)
    {
        _ipSets.Clear();
        _others.Clear();

        foreach (var address in addresses)
        {
            var isLoaded = CidrIpSet.TryParse(address, out var ipSet)
                ? _ipSets.TryAdd(ipSet, Stopwatch.GetTimestamp())
                : _others.Add(address);

            if (isLoaded)
                OnAddressLoad(address);
        }

        return autoCollapse && Collapse();
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

        if (!_ipSets.TryAdd(new CidrIpSet(ipAddress), Stopwatch.GetTimestamp()))
            return false;

        OnAddressAdded(ipAddress.ToString());

        if (autoCollapse)
            Collapse();

        var ipSetMaxCount = maxCount - _others.Count;
        while (_ipSets.Count > ipSetMaxCount)
        {
            var oldIpSet = _ipSets.MinBy(x => x.Value).Key;
            _ipSets.Remove(oldIpSet, out _);
            OnAddressRemove(oldIpSet.Address);
        }

        return true;
    }

    public override IReadOnlyCollection<string> GetOrdered()
    {
        return _others
            .Union(_ipSets.OrderBy(x => x.Value).Select(x => x.Key.Address))
            .ToList();
    }

    private bool Collapse()
    {
        var changed = false;

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
            _ipSets.Add(merged, Stopwatch.GetTimestamp());
            changed = true;

            i -= Math.Min(i, 2);

            OnAddressMerge([current.Address, next.Address], merged.Address);
        }

        return changed;
    }
}
