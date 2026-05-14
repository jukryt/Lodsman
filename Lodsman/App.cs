using System.Net;
using System.Reflection;
using Lodsman.AddressSaver;
using Lodsman.Log;
using Lodsman.Network;
using Lodsman.Shutdown;
using NetTools;

namespace Lodsman;

internal class App(IConfig config, ILog log)
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

        log.Info("Init...");
        using var context = await ContextBuilder.BuildAsync(config, log);
        using var shutdownProcessor = new PosixShutdownProcessor(context.ShutdownAction, log);
        var addressSaverProcessor = new AddressSaverProcessor(context.AddressSaverAction, log, shutdownProcessor.CancellationToken);

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

            log.Info($"{address} - loaded");
        }

        using (var listener = TraceEventListener.Start())
        {
            listener.Connection += (s, e) => ConnectionHandler(s, e, addressSaverProcessor);
            log.Info("Ready...");
            await shutdownProcessor.EndWorkAwaiter;
            log.Info("Shutdown...");
        }

        return await shutdownProcessor.ExitAppAwaiter;
    }

    private void ConnectionHandler(object? sender, ConnectionEventArgs e, AddressSaverProcessor addressSaverProcessor)
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
        log.Info($"{address} - added");

        var addressesMaxCount = addressSaverProcessor.MaxAddressCount - _domains.Count - _addressesRanges.Count;
        while (_addresses.Count > addressesMaxCount)
        {
            var oldAddress = _addresses.MinBy(x => x.Value).Key;
            _addresses.Remove(oldAddress);
            log.Info($"{oldAddress} - remove");
        }

        var addresses = _domains
            .Union(_addressesRanges.Keys)
            .Union(_addresses.Keys.Order())
            .ToList();

        addressSaverProcessor.Save(addresses);
    }
}
