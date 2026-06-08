using System.Text.Json;

namespace Lodsman.Json;

internal static class JsonOptions
{
    public static JsonSerializerOptions Instance { get; } = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.KebabCaseLower,
        WriteIndented = true,
        NewLine = "\n",
        IndentSize = 4
    }.WithBaseTypeAttribute();
}
