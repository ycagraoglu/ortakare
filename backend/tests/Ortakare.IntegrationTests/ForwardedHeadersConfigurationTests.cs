using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Ortakare.Api.Infrastructure.Proxy;
using AspNetForwardedHeadersOptions = Microsoft.AspNetCore.Builder.ForwardedHeadersOptions;

namespace Ortakare.IntegrationTests;

public sealed class ForwardedHeadersConfigurationTests
{
    [Fact]
    public void Trusted_proxy_configuration_enables_forwarded_for_and_proto()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ForwardedHeaders:Enabled"] = "true",
                ["ForwardedHeaders:KnownProxies:0"] = "127.0.0.1",
                ["ForwardedHeaders:ForwardLimit"] = "1"
            })
            .Build();
        var services = new ServiceCollection();

        services.AddTrustedForwardedHeaders(configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AspNetForwardedHeadersOptions>>().Value;

        Assert.Equal(ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto, options.ForwardedHeaders);
        Assert.Equal(1, options.ForwardLimit);
        Assert.Contains(global::System.Net.IPAddress.Loopback, options.KnownProxies);
    }

    [Fact]
    public void Disabled_configuration_does_not_trust_forwarded_headers()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ForwardedHeaders:Enabled"] = "false"
            })
            .Build();
        var services = new ServiceCollection();

        services.AddTrustedForwardedHeaders(configuration);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<AspNetForwardedHeadersOptions>>().Value;

        Assert.Equal(ForwardedHeaders.None, options.ForwardedHeaders);
    }
}
