namespace Lodsman.AddressSaver;

internal interface IAddressSaverProcessor
{
    int MaxAddressCount { get; }
    void Save(IReadOnlyCollection<string> addresses);
}
