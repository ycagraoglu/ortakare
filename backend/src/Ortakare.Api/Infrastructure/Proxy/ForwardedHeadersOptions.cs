namespace Ortakare.Api.Infrastructure.Proxy;

public sealed class ForwardedHeadersOptions
{
    public const string SectionName = "ForwardedHeaders";

    public bool Enabled { get; init; }
    public string[] KnownProxies { get; init; } = [];
    public string[] KnownNetworks { get; init; } = [];
    public int ForwardLimit { get; init; } = 1;
}
