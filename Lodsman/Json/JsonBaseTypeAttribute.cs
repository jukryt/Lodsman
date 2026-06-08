namespace Lodsman.Json;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
internal class JsonBaseTypeAttribute : Attribute
{
    public required Type BaseType { get; init; }
    public required string TypeDiscriminator { get; init; }
}
