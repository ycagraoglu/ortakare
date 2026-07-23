using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Ortakare.IntegrationTests.Health;

public sealed class HealthEndpointTests : IClassFixture<OrtakareApiFactory>
{
    private readonly OrtakareApiFactory _factory;

    public HealthEndpointTests(OrtakareApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Live_returns_healthy_without_dependency_checks(CancellationToken cancellationToken)
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/live", cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("no-store", response.Headers.CacheControl?.ToString(), StringComparison.OrdinalIgnoreCase);

        var payload = await response.Content.ReadFromJsonAsync<HealthResponse>(cancellationToken);
        Assert.NotNull(payload);
        Assert.Equal(HealthStatus.Healthy.ToString(), payload.Status);
    }

    [Fact]
    public async Task Protected_api_endpoint_returns_unauthorized_without_access_token(
        CancellationToken cancellationToken)
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/events", cancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private sealed record HealthResponse(string Status, DateTime TimestampUtc);
}