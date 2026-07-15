using System.Net;
using System.Text.Json;

namespace Ortakare.IntegrationTests.HealthChecks;

public sealed class HealthCheckTests : IClassFixture<OrtakareApiFactory>
{
    private readonly OrtakareApiFactory _factory;

    public HealthCheckTests(OrtakareApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Application_health_endpoint_reports_healthy_without_dependency_checks(CancellationToken cancellationToken)
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/application", cancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        Assert.Equal("Healthy", json.RootElement.GetProperty("status").GetString());
        Assert.Empty(json.RootElement.GetProperty("checks").EnumerateArray());
    }
}