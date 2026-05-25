using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using Lodsman.Log;
using NetTools;

namespace Lodsman.Main;

internal class AddressCollection(int maxCount, ILog log)
{
    private readonly Dictionary<string, IPAddressRange> _ipRanges = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, long> _ips = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _others = new(StringComparer.OrdinalIgnoreCase);

    public void Init(IReadOnlyCollection<string> addresses)
    {
        foreach (var address in addresses)
        {
            if (IPAddressRange.TryParse(address, out var ipAddressRange))
            {
                if (ipAddressRange.AddressCount > 1)
                    _ipRanges.TryAdd(address, ipAddressRange);
                else
                    _ips.TryAdd(address, Stopwatch.GetTimestamp());

                log.Info($"{address} - loaded");
            }
            else
                _others.Add(address);
        }

        log.Info($"Addresses loaded: {Count}");
    }

    public int Count => _ipRanges.Count + _ips.Count + _others.Count;

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
