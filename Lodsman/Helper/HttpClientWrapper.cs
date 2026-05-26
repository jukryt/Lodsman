using Lodsman.Extension;

namespace Lodsman.Helper;

internal class HttpClientWrapper(HttpClient client)
{
    public async Task<T> CallAsync<T>(Func<HttpClient, Task<T>> method)
    {
        try
        {
            return await method(client);
        }
        catch (HttpRequestException ex) when (ex.InnerException is OperationCanceledException)
        {
            ex.InnerException.Rethrow();
            throw;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == null)
        {
            throw new HttpConnectException(ex);
        }
        catch (HttpRequestException ex)
        {
            throw new HttpResponseException(ex);
        }
        catch (Exception ex)
        {
            ex.Rethrow();
            throw;
        }
    }
}

internal class HttpConnectException(HttpRequestException source) : HttpRequestException((source.InnerException ?? source).Message, source);
internal class HttpResponseException(HttpRequestException source) : HttpRequestException($"{source.StatusCode} - {(source.InnerException ?? source).Message}", source);
