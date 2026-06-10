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
    private readonly AddressCollection _addresses;
    private readonly AsyncActionThrottler<IReadOnlyCollection<string>> _saveAction;

    public App(IContext context)
    {
        _context = context;
        _addresses = new AddressCollection(context.MaxAddressCount, context.ShowAddressesOnLoad, context.Log);
        _processNames = new ReadOnlySet<string>(new HashSet<string>(context.ProcessNames, StringComparer.OrdinalIgnoreCase));
        _saveAction = new AsyncActionThrottler<IReadOnlyCollection<string>>(context.SaveAsync, context.SavingDelay, context.Log);

        _saveAction.Complete += SaveComplete;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _addresses.Init(_context.Addresses);

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

        var addresses = _addresses.GetAll();
        _saveAction.Run(addresses, cancellationToken);
    }

    private void SaveComplete(object? sender, EventArgs e)
    {
        _context.Log.Info("Save complete");
    }
}
