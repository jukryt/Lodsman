namespace Lodsman.AddressSaver;

internal interface IAddressSaverAction
{
    int MaxAddressCount { get; }
    Task SaveAsync(IReadOnlyCollection<string> addresses, CancellationToken cancellationToken);
}
