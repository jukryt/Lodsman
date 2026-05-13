using System.Net;

namespace Lodsman;

internal static class HttpClientHelper
{
    public static HttpClient Instance { get; } = Create();

    private static HttpClient Create()
    {
        var client = new HttpClient(new HttpClientHandler { CookieContainer = new CookieContainer() });
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"{App.Name}/{App.Version}");
        return client;
    }
}
