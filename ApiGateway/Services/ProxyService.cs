namespace ApiGateway.Services;

public class ProxyService : IProxyService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProxyService> _logger;

    public ProxyService(HttpClient httpClient, ILogger<ProxyService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<HttpResponseMessage> ForwardAsync(HttpRequest incomingRequest)
    {
        // Build the target URL: same path + query string, against the backend base address
        var targetUri = new Uri(_httpClient.BaseAddress!, incomingRequest.Path + incomingRequest.QueryString);

        var forwardRequest = new HttpRequestMessage
        {
            Method = new HttpMethod(incomingRequest.Method),
            RequestUri = targetUri
        };

        // Copy the body, if any (e.g. POST requests)
        if (incomingRequest.ContentLength > 0)
        {
            forwardRequest.Content = new StreamContent(incomingRequest.Body);
            if (incomingRequest.ContentType is not null)
            {
                forwardRequest.Content.Headers.TryAddWithoutValidation("Content-Type", incomingRequest.ContentType);
            }
        }

        _logger.LogInformation("Forwarding {Method} {Path} to {TargetUri}",
            incomingRequest.Method, incomingRequest.Path, targetUri);

        var response = await _httpClient.SendAsync(forwardRequest);

        return response;
    }
}