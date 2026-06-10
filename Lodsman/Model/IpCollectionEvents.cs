namespace Lodsman.Model;

internal class AddressEventArgs(string address) : EventArgs
{
    public string Address { get; } = address;
}

internal class MergeEventArgs(string[] source, string target) : EventArgs
{
    public string[] SourceAddresses { get; } = source;
    public string TargetAddress { get; } = target;
}
