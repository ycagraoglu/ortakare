using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Ortakare.IntegrationTests.System;

public sealed class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_returns_success(CancellationToken cancellationToken)
    {
        var response = await _client.GetAsync("/api/system/health", cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
