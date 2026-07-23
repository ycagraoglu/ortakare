using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using AspNetForwardedHeadersOptions = Microsoft.AspNetCore.Builder.ForwardedHeadersOptions;
using AspNetIPNetwork = System.Net.IPNetwork;

namespace Ortakare.Api.Infrastructure.Proxy;

public static class ForwardedHeadersConfiguration
{
    public static IServiceCollection AddTrustedForwardedHeaders(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ForwardedHeadersOptions>()
            .BindConfiguration(ForwardedHeadersOptions.SectionName)
            .Validate(x => x.ForwardLimit is > 0 and <= 10, "ForwardedHeaders:ForwardLimit must be between 1 and 10.")
            .Validate(x => !x.Enabled || x.KnownProxies.Length > 0 || x.KnownNetworks.Length > 0,
                "ForwardedHeaders must contain at least one trusted proxy or network when enabled.")
            .Validate(x => x.KnownProxies.All(value => IPAddress.TryParse(value, out _)),
                "ForwardedHeaders:KnownProxies must contain valid IP addresses.")
            .Validate(x => x.KnownNetworks.All(IsValidNetwork),
                "ForwardedHeaders:KnownNetworks must contain valid CIDR networks.")
            .ValidateOnStart();

        services.AddOptions<AspNetForwardedHeadersOptions>()
            .Configure<IOptions<ForwardedHeadersOptions>>((options, configuredOptions) =>
            {
                var configured = configuredOptions.Value;
                if (!configured.Enabled)
                {
                    options.ForwardedHeaders = ForwardedHeaders.None;
                    return;
                }

                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                options.ForwardLimit = configured.ForwardLimit;
                options.KnownProxies.Clear();
                options.KnownIPNetworks.Clear();

                foreach (var proxy in configured.KnownProxies)
                    options.KnownProxies.Add(IPAddress.Parse(proxy));

                foreach (var network in configured.KnownNetworks)
                {
                    var parts = network.Split('/', 2);
                    options.KnownIPNetworks.Add(new AspNetIPNetwork(IPAddress.Parse(parts[0]), int.Parse(parts[1])));
                }
            });

        return services;
    }

    private static bool IsValidNetwork(string value)
    {
        var parts = value.Split('/', 2);
        if (parts.Length != 2 || !IPAddress.TryParse(parts[0], out var address) || !int.TryParse(parts[1], out var prefixLength))
            return false;

        var maximumPrefixLength = address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? 32 : 128;
        return prefixLength is >= 0 && prefixLength <= maximumPrefixLength;
    }
}
