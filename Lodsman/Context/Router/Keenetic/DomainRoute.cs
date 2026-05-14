namespace Lodsman.Context.Router.Keenetic;

internal record DomainRoute
{
    public required string Key { get; init; }
    public required string ListName { get; init; }
    public required List<string> Addresses { get; init; }
}
