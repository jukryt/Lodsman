using System.Net;
using System.Reflection;
using Lodsman.AddressSaver;
using Lodsman.Network;
using Lodsman.Shutdown;
using NetTools;

namespace Lodsman;

internal class App(IConfig config)
{
    public static string Name => nameof(Lodsman);
    public static Version Version { get; } = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0);

    private readonly HashSet<string> _processNames = new (StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _domains = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IPAddressRange> _addressesRanges = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, DateTime> _addresses = new(StringComparer.OrdinalIgnoreCase);

    public async Task<int> RunAsync()
    {
        config.ProcessNames.ForEach(n => _processNames.Add(n));
        Console.Title = $"{Name} - {string.Join(", ", _processNames)}";

        Console.WriteLine("Init...");
        using var context = await ContextBuilder.BuildAsync(config);
        using var shutdownProcessor = new ShutdownProcessor(context.ShutdownAction);
        var addressSaverProcessor = new AddressSaverProcessor(context.AddressSaverAction, shutdownProcessor.CancellationToken);

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

            Console.WriteLine($"{address} - loaded");
        }

        using (var listener = TraceEventListener.Start())
        {
            listener.Connection += (_, e) => ConnectionHandler(e.ProcessName, e.Info, addressSaverProcessor);
            Console.WriteLine("Ready...");
            await shutdownProcessor.EndWorkAwaiter;
            Console.WriteLine("Shutdown...");
        }

        return await shutdownProcessor.ExitAppAwaiter;
    }

    private void ConnectionHandler(string processName, ConnectionInfo info, AddressSaverProcessor saverProcessor)
    {
        if (string.IsNullOrEmpty(processName) ||
            !_processNames.Contains(processName))
            return;

        var targetIp = info.TargetIp;

        if (IPAddress.IsLoopback(targetIp))
            return;

        if (_addressesRanges.Values.Any(r => r.Contains(targetIp)))
            return;

        var address = targetIp.ToString();
        if (_addresses.ContainsKey(address))
            return;

        _addresses.Add(address, DateTime.Now);
        Console.WriteLine($"{address} - added");

        var addressesMaxCount = saverProcessor.MaxAddressCount - _domains.Count - _addressesRanges.Count;
        while (_addresses.Count > addressesMaxCount)
        {
            var oldAddress = _addresses.MinBy(x => x.Value).Key;
            _addresses.Remove(oldAddress);
            Console.WriteLine($"{oldAddress} - remove");
        }

        var addresses = _domains
            .Union(_addressesRanges.Keys)
            .Union(_addresses.Keys.Order())
            .ToList();

        saverProcessor.Save(addresses);
    }
}
