using System.Collections.ObjectModel;
using System.Net;
using System.Reflection;
using Lodsman.Context;
using Lodsman.Extension;
using Lodsman.Helper;
using Lodsman.Model;
using Lodsman.Network.MicrosoftTraceEvent;

namespace Lodsman;

internal class App
{
    public static string Name => nameof(Lodsman);
    public static Version Version { get; } = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0);

    private readonly IContext _context;
    private readonly ReadOnlySet<string> _processNames;
    private readonly IAddressCollection _addresses;
    private readonly AsyncActionThrottler<IReadOnlyCollection<string>> _saveAction;

    public App(IContext context)
    {
        _context = context;
        _processNames = new ReadOnlySet<string>(new HashSet<string>(context.ProcessNames, StringComparer.OrdinalIgnoreCase));
        _addresses = context.CreateAddressCollection();
        _saveAction = new AsyncActionThrottler<IReadOnlyCollection<string>>(context.SaveAsync, context.SavingDelay, context.Log);

        _addresses.AddressLoad += AddressLoad;
        _addresses.AddressAdded += AddressAdded;
        _addresses.AddressRemove += AddressRemove;
        _addresses.AddressMerge += AddressMerge;
        _saveAction.Complete += SaveComplete;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var changed = _addresses.Init(_context.Addresses);
        _context.Log.Info($"Total addresses: {_addresses.Count}. Maximum: {_addresses.MaxCount}");

        if (changed)
        {
            var addresses = _addresses.GetOrdered();
            _saveAction.Run(addresses, cancellationToken);
        }

        using var listener = TraceEventListener.Start();
        listener.IpSend += (_, e) => IpSendHandler(e.ProcessName, e.TargetIp, cancellationToken);

        _context.Log.Info("Ready...");
        await TaskExtension.AwaitTokenAsync(cancellationToken);
    }

    public async Task ShutdownAsync()
    {
        await _context.ShutdownAsync();
    }

    private void IpSendHandler(string processName, IPAddress targetIp, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
            return;

        if (string.IsNullOrEmpty(processName) ||
            !_processNames.Contains(processName))
            return;

        if (IPAddress.IsLoopback(targetIp))
            return;

        if (!_addresses.TryAdd(targetIp))
            return;

        var addresses = _addresses.GetOrdered();
        _saveAction.Run(addresses, cancellationToken);
    }

    private void AddressLoad(object? sender, AddressEventArgs e)
    {
        if (_context.ShowAddressesOnLoad)
            _context.Log.Info($"{e.Address} - loaded");
    }

    private void AddressAdded(object? sender, AddressEventArgs e)
    {
        _context.Log.Info($"{e.Address} - added");
    }

    private void AddressRemove(object? sender, AddressEventArgs e)
    {
        _context.Log.Info($"{e.Address} - remove");
    }

    private void AddressMerge(object? sender, MergeEventArgs e)
    {
        _context.Log.Info($"{e.TargetAddress} - merge from: {string.Join(", ", e.SourceAddresses)}");
    }

    private void SaveComplete(object? sender, EventArgs e)
    {
        _context.Log.Info("Save complete");
    }
}
