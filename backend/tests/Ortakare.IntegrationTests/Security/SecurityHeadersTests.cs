using System.Net;

namespace Ortakare.IntegrationTests.Security;

public sealed class SecurityHeadersTests : IClassFixture<OrtakareApiFactory>
{
    private readonly OrtakareApiFactory _factory;

    public SecurityHeadersTests(OrtakareApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Api_response_contains_security_headers(CancellationToken cancellationToken)
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/public/events/unknown-gallery-token", cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("nosniff", GetHeader(response, "X-Content-Type-Options"));
        Assert.Equal("no-referrer", GetHeader(response, "Referrer-Policy"));
        Assert.Equal("DENY", GetHeader(response, "X-Frame-Options"));
        Assert.Equal("default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'", GetHeader(response, "Content-Security-Policy"));
        Assert.Equal("camera=(), microphone=(), geolocation=(), payment=(), usb=()", GetHeader(response, "Permissions-Policy"));
    }

    private static string GetHeader(HttpResponseMessage response, string name)
    {
        Assert.True(response.Headers.TryGetValues(name, out var values));
        return Assert.Single(values);
    }
}
