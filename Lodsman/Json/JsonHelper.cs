using System.Text.Json;

namespace Lodsman.Json
{
    internal static class JsonHelper
    {
        public static async Task SerializeAsync<T>(Stream stream, T value, CancellationToken cancellationToken = default)
        {
            await JsonSerializer.SerializeAsync(stream, value, JsonOptions.Instance, cancellationToken);
        }

        public static async Task<T?> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken = default)
        {
            return await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions.Instance, cancellationToken);
        }
    }
}
