using System.Net;

namespace Lodsman.Model;

internal abstract class BaseAddressCollection : IAddressCollection
{
    public abstract int Count { get; }
    public abstract int MaxCount { get; }

    public event EventHandler<AddressEventArgs>? AddressLoad;
    public event EventHandler<AddressEventArgs>? AddressAdded;
    public event EventHandler<AddressEventArgs>? AddressRemove;
    public event EventHandler<MergeEventArgs>? AddressMerge;

    public abstract bool Init(IReadOnlyCollection<string> addresses);
    public abstract bool TryAdd(IPAddress ipAddress);
    public abstract IReadOnlyCollection<string> GetOrdered();

    protected void OnAddressLoad(string address) => AddressLoad?.Invoke(this, new AddressEventArgs(address));
    protected void OnAddressAdded(string address) => AddressAdded?.Invoke(this, new AddressEventArgs(address));
    protected void OnAddressRemove(string address) => AddressRemove?.Invoke(this, new AddressEventArgs(address));
    protected void OnAddressMerge(string[] source, string target) => AddressMerge?.Invoke(this, new MergeEventArgs(source, target));
}
