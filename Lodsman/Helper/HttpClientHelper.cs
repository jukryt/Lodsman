using System.Net;

namespace Lodsman.Helper;

internal static class HttpClientHelper
{
    private static HttpClient Instance { get; } = Create();
    public static HttpClientWrapper Wrapper { get; } = new HttpClientWrapper(Instance);

    private static HttpClient Create()
    {
        var client = new HttpClient(new HttpClientHandler { CookieContainer = new CookieContainer() });
        client.DefaultRequestHeaders.UserAgent.ParseAdd($"{App.Name}/{App.Version}");
        return client;
    }
}
