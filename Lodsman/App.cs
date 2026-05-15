using System.Net;
using System.Reflection;
using Lodsman.Context;
using Lodsman.Extension;
using Lodsman.Network;
using NetTools;

namespace Lodsman;

internal class App
{
    public static string Name => nameof(Lodsman);
    public static Version Version { get; } = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0);

    private readonly IContext _context;
    private readonly HashSet<string> _processNames = new (StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _domains = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IPAddressRange> _addressesRanges = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DateTime> _addresses = new(StringComparer.OrdinalIgnoreCase);
    private readonly AsyncActionThrottler<IReadOnlyCollection<string>> _saveThrottler;

    public App(IContext context)
    {
        _context = context;
        _saveThrottler = new AsyncActionThrottler<IReadOnlyCollection<string>>(_context.SaveAsync, SaveComplete, context.Log);

        foreach (var processName in _context.ProcessNames)
            _processNames.Add(processName);
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        //System.Diagnostics.Debugger.Launch();

        _context.Log.Info("Init...");

        foreach (var address in _context.Addresses)
        {
            if (IPAddressRange.TryParse(address, out var ipAddressRange))
            {
                if (ipAddressRange.AddressCount > 1)
                    _addressesRanges.Add(address, ipAddressRange);
                else
                    _addresses.Add(address, DateTime.Now);
            }
            else
                _domains.Add(address);

            _context.Log.Info($"{address} - loaded");
        }

        using var listener = TraceEventListener.Start();
        listener.Connection += (_, e) => ConnectionHandler(e.ProcessName, e.TargetIp, cancellationToken);

        _context.Log.Info("Ready...");
        await TaskExtension.AwaitTokenAsync(cancellationToken);
        _context.Log.Info("Shutdown...");
    }

    public async Task ShutdownAsync()
    {
        await _context.ShutdownAsync();
    }

    private void ConnectionHandler(string processName, IPAddress targetIp, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(processName) ||
            !_processNames.Contains(processName))
            return;

        if (IPAddress.IsLoopback(targetIp))
            return;

        if (_addressesRanges.Values.Any(r => r.Contains(targetIp)))
            return;

        var address = targetIp.ToString();
        if (_addresses.ContainsKey(address))
            return;

        _addresses.Add(address, DateTime.Now);
        _context.Log.Info($"{address} - added");

        var addressesMaxCount = _context.MaxAddressCount - _domains.Count - _addressesRanges.Count;
        while (_addresses.Count > addressesMaxCount)
        {
            var oldAddress = _addresses.MinBy(x => x.Value).Key;
            _addresses.Remove(oldAddress);
            _context.Log.Info($"{oldAddress} - remove");
        }

        var addresses = _domains
            .Union(_addressesRanges.Keys)
            .Union(_addresses.Keys.Order())
            .ToList();

        _saveThrottler.Run(addresses, cancellationToken);
    }

    private void SaveComplete()
    {
        _context.Log.Info("Save complete");
    }
}
