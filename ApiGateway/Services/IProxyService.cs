namespace ApiGateway.Services;

public interface IProxyService
{
    Task<HttpResponseMessage> ForwardAsync(HttpRequest incomingRequest);
}