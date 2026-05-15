using System.Net;
using System.Reflection;
using Lodsman.AddressSaver;
using Lodsman.Context;
using Lodsman.Extension;
using Lodsman.Network;
using NetTools;

namespace Lodsman;

internal class App(IContext context, IAddressSaverProcessor addressSaverProcessor)
{
    public static string Name => nameof(Lodsman);
    public static Version Version { get; } = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0);

    private readonly HashSet<string> _processNames = new (StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _domains = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IPAddressRange> _addressesRanges = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DateTime> _addresses = new(StringComparer.OrdinalIgnoreCase);

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        //System.Diagnostics.Debugger.Launch();

        context.Log.Info("Init...");

        foreach (var processName in context.ProcessNames)
            _processNames.Add(processName);

        foreach (var address in context.Addresses)
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

            context.Log.Info($"{address} - loaded");
        }

        using var listener = TraceEventListener.Start();
        listener.Connection += ConnectionHandler;

        context.Log.Info("Ready...");
        await TaskExtension.AwaitTokenAsync(cancellationToken);
        context.Log.Info("Shutdown...");
    }

    private void ConnectionHandler(object? sender, ConnectionEventArgs e)
    {
        if (string.IsNullOrEmpty(e.ProcessName) ||
            !_processNames.Contains(e.ProcessName))
            return;

        var targetIp = e.TargetIp;

        if (IPAddress.IsLoopback(targetIp))
            return;

        if (_addressesRanges.Values.Any(r => r.Contains(targetIp)))
            return;

        var address = targetIp.ToString();
        if (_addresses.ContainsKey(address))
            return;

        _addresses.Add(address, DateTime.Now);
        context.Log.Info($"{address} - added");

        var addressesMaxCount = addressSaverProcessor.MaxAddressCount - _domains.Count - _addressesRanges.Count;
        while (_addresses.Count > addressesMaxCount)
        {
            var oldAddress = _addresses.MinBy(x => x.Value).Key;
            _addresses.Remove(oldAddress);
            context.Log.Info($"{oldAddress} - remove");
        }

        var addresses = _domains
            .Union(_addressesRanges.Keys)
            .Union(_addresses.Keys.Order())
            .ToList();

        addressSaverProcessor.Save(addresses);
    }
}
