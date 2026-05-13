using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace Lodsman.Router.Keenetic;

internal class KeeneticApi
{
    public const int MaxDomainRoutes = 300;

    private readonly HttpClient _client;
    private readonly Uri _baseUri;
    private readonly string _user;
    private readonly string _password;

    public KeeneticApi(HttpClient client, string address, string user, string password)
    {
        _client = client;
        _baseUri = new UriBuilder(address).Uri;
        _user = user;
        _password = password;
    }

    public async Task LoginAsync(CancellationToken cancellationToken = default)
    {
        var authUri = new Uri(_baseUri, "auth");
        var authResponse = await _client.GetAsync(authUri, cancellationToken);
        if (authResponse.IsSuccessStatusCode)
            return;

        var realm = authResponse.Headers.TryGetValues("x-ndm-realm", out var realmValues) ? realmValues.FirstOrDefault() : string.Empty;
        var challenge = authResponse.Headers.TryGetValues("x-ndm-challenge", out var challengeValues) ? challengeValues.FirstOrDefault() : string.Empty;
        var authData = _user + ":" + realm + ":" + _password;
        var authMd5 = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(authData))).ToLower();
        var passwordData = challenge + authMd5;
        var passwordSha256 = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(passwordData))).ToLower();

        var loginRequestData = new JsonObject{
            {"login", _user},
            {"password", passwordSha256},
        };

        var loginRequestContent = new StringContent(loginRequestData.ToJsonString(), Encoding.UTF8, "application/json");
        var loginResponse = await _client.PostAsync(authUri, loginRequestContent, cancellationToken);
        if (!loginResponse.IsSuccessStatusCode)
            throw new Exception($"Keenetic auth error: {(int)loginResponse.StatusCode}");
    }

    public async Task<DomainRoute> GetDomainRouteAsync(string listName, CancellationToken cancellationToken = default)
    {
        var request = new JsonArray(
            new JsonObject {
                {"show", new JsonObject {
                    {"sc", new JsonObject {
                        {"object-group", new JsonObject {
                            {"fqdn", new JsonObject()},
                        }},
                    }},
                }},
            }
        );

        var response = await SendRequestAsync(request, cancellationToken);
        var fqdns = response?.AsArray()?[0]?["show"]?["sc"]?["object-group"]?["fqdn"]?.AsObject();
        if (fqdns == null)
            throw new Exception($"Unknown response format: \"{response}\".");

        foreach (var fqdn in fqdns)
        {
            if (fqdn.Value == null)
                continue;

            if (fqdn.Value["description"]?.ToString() != listName)
                continue;

            var includes = fqdn.Value["include"]?.AsArray();
            if (includes == null)
                includes = [];

            var addresses = new List<string>();
            foreach (var include in includes)
            {
                if (include == null)
                    continue;

                var address = include["address"]?.ToString();
                if (string.IsNullOrEmpty(address))
                    continue;

                addresses.Add(address);
            }

            return new DomainRoute
            {
                Key = fqdn.Key,
                ListName = listName,
                Addresses = addresses
            };
        }

        throw new Exception($"Domain route list '{listName}' not found.");
    }

    public async Task SaveDomainRouteAsync(DomainRoute domainRoute, CancellationToken cancellationToken = default)
    {
        if (domainRoute.Addresses.Count > MaxDomainRoutes)
            throw new Exception($"Too many domain routes: {domainRoute.Addresses.Count}/{MaxDomainRoutes}.");

        var addresses = domainRoute.Addresses
            .Select(x => new JsonObject {
                {"address", x},
            });

        var request = new JsonArray(
            new JsonObject {
                {"object-group", new JsonObject {
                    {"fqdn", new JsonObject {
                        {domainRoute.Key, new JsonObject {
                            {"include", new JsonObject {
                                {"no", true}
                            }},
                        }}
                    }},
                }},
            },
            new JsonObject {
                {"object-group", new JsonObject {
                    {"fqdn", new JsonObject {
                        {domainRoute.Key, new JsonObject {
                            {"include", new JsonArray([.. addresses])},
                        }}
                    }},
                }},
            },
            new JsonObject {
                {"system", new JsonObject {
                    {"configuration", new JsonObject {
                        {"save", new JsonObject()},
                    }},
                }},
            }
        );

        await SendRequestAsync(request, cancellationToken);
    }

    private async Task<JsonNode> SendRequestAsync(JsonNode request, CancellationToken cancellationToken)
    {
        var rciUri = new Uri(_baseUri, "rci/");
        var requestContent = new StringContent(request.ToJsonString(), Encoding.UTF8, "application/json");
        var response = await _client.PostAsync(rciUri, requestContent, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await LoginAsync(cancellationToken);
            response = await _client.PostAsync(rciUri, requestContent, cancellationToken);
        }

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Request error: {(int)response.StatusCode}");

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrEmpty(responseJson))
            throw new NullReferenceException(nameof(responseJson));

        return JsonNode.Parse(responseJson) ?? throw new Exception($"Response not parse: \"{responseJson}\".");
    }
}
