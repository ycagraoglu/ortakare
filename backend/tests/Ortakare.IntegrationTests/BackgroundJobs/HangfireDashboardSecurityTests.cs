using System.Net;

namespace Ortakare.IntegrationTests.BackgroundJobs;

public sealed class HangfireDashboardSecurityTests : IClassFixture<OrtakareApiFactory>
{
    private readonly OrtakareApiFactory _factory;

    public HangfireDashboardSecurityTests(OrtakareApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Dashboard_is_not_exposed_when_Hangfire_is_disabled(CancellationToken cancellationToken)
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/operations/jobs", cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
