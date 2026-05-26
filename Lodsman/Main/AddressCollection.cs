using System.Diagnostics;
using System.Net;
using Lodsman.Log;
using NetTools;

namespace Lodsman.Main;

internal class AddressCollection(int maxCount, bool showAddressesOnLoad, ILog log)
{
    private readonly Dictionary<string, IPAddressRange> _ipRanges = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, long> _ips = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _others = new(StringComparer.OrdinalIgnoreCase);

    public int Count => _ipRanges.Count + _ips.Count + _others.Count;

    public void Init(IReadOnlyCollection<string> addresses)
    {
        _ipRanges.Clear();
        _ips.Clear();
        _others.Clear();

        foreach (var address in addresses)
        {
            if (IPAddressRange.TryParse(address, out var ipAddressRange))
            {
                if (ipAddressRange.AddressCount > 1)
                    _ipRanges.TryAdd(address, ipAddressRange);
                else
                    _ips.TryAdd(address, Stopwatch.GetTimestamp());
            }
            else
                _others.Add(address);

            if (showAddressesOnLoad)
                log.Info($"{address} - loaded");
        }

        log.Info($"Total addresses: {Count}. Maximum: {maxCount}");
    }

    public bool TryAdd(IPAddress ipAddress)
    {

        if (_ipRanges.Values.Any(r => r.Contains(ipAddress)))
            return false;

        var address = ipAddress.ToString();
        if (!_ips.TryAdd(address, Stopwatch.GetTimestamp()))
            return false;

        log.Info($"{address} - added");

        var ipMaxCount = maxCount - _ipRanges.Count - _others.Count;
        while (_ips.Count > ipMaxCount)
        {
            var oldAddress = _ips.MinBy(x => x.Value).Key;
            _ips.Remove(oldAddress, out _);
            log.Info($"{oldAddress} - remove");
        }

        return true;
    }

    public IReadOnlyCollection<string> GetAll()
    {
        return _others
            .Union(_ipRanges.Keys)
            .Union(_ips.Keys)
            .ToList();
    }
}
