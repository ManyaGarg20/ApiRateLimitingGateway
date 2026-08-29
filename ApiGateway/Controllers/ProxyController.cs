using Microsoft.AspNetCore.Mvc;
using ApiGateway.Services;

namespace ApiGateway.Controllers;

[ApiController]
[Route("api/{**catchAll}")]
public class ProxyController : ControllerBase
{
    private readonly IProxyService _proxyService;

    public ProxyController(IProxyService proxyService)
    {
        _proxyService = proxyService;
    }

    [HttpGet]
    [HttpPost]
    [HttpPut]
    [HttpDelete]
    public async Task<IActionResult> Proxy(string catchAll)
    {
        var backendResponse = await _proxyService.ForwardAsync(Request);

        var content = await backendResponse.Content.ReadAsByteArrayAsync();
        var contentType = backendResponse.Content.Headers.ContentType?.ToString() ?? "application/json";

        return new ContentResult
        {
            Content = System.Text.Encoding.UTF8.GetString(content),
            ContentType = contentType,
            StatusCode = (int)backendResponse.StatusCode
        };
    }
}