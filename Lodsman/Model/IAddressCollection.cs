using System.Net;

namespace Lodsman.Model;

internal interface IAddressCollection
{
    int Count { get; }
    int MaxCount { get; }

    event EventHandler<AddressEventArgs>? AddressLoad;
    event EventHandler<AddressEventArgs>? AddressAdded;
    event EventHandler<AddressEventArgs>? AddressRemove;
    event EventHandler<MergeEventArgs>? AddressMerge;

    bool Init(IEnumerable<string> addresses);
    bool TryAdd(IPAddress ipAddress);
    IReadOnlyCollection<string> GetAll();
    IReadOnlyCollection<string> GetOrdered();
}
