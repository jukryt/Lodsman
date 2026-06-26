using System.Diagnostics;
using System.Net;

namespace Lodsman.Model;

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

                if (j < i) i--;
                j--;

                OnAddressRemove(challenger.Address);
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
