using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Ortakare.Api.Infrastructure.Observability;

public static class ObservabilityServiceCollectionExtensions
{
    public static IServiceCollection AddOrtakareObservability(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        services.AddOptions<ObservabilityOptions>()
            .BindConfiguration(ObservabilityOptions.SectionName)
            .Validate(x => !string.IsNullOrWhiteSpace(x.ServiceName),
                "Observability:ServiceName is required.")
            .Validate(x => x.TraceSampleRatio is >= 0 and <= 1,
                "Observability:TraceSampleRatio must be between 0 and 1.")
            .Validate(x => string.IsNullOrWhiteSpace(x.OtlpEndpoint) ||
                           Uri.TryCreate(x.OtlpEndpoint, UriKind.Absolute, out _),
                "Observability:OtlpEndpoint must be an absolute URI.")
            .ValidateOnStart();

        var options = configuration
            .GetSection(ObservabilityOptions.SectionName)
            .Get<ObservabilityOptions>() ?? new ObservabilityOptions();

        services.AddSingleton<OrtakareTelemetry>();

        if (!options.Enabled)
        {
            return services;
        }

        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(
                serviceName: options.ServiceName,
                serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString(),
                serviceInstanceId: Environment.MachineName)
            .AddAttributes(new Dictionary<string, object>
            {
                ["deployment.environment.name"] = environment.EnvironmentName
            });

        var openTelemetry = services.AddOpenTelemetry()
            .ConfigureResource(builder => builder.AddService(options.ServiceName))
            .WithTracing(tracing =>
            {
                tracing
                    .SetResourceBuilder(resourceBuilder)
                    .SetSampler(new ParentBasedSampler(
                        new TraceIdRatioBasedSampler(options.TraceSampleRatio)))
                    .AddSource(OrtakareTelemetry.ActivitySourceName)
                    .AddSource("Npgsql")
                    .AddAspNetCoreInstrumentation(instrumentation =>
                    {
                        instrumentation.RecordException = true;
                        instrumentation.Filter = context =>
                            !context.Request.Path.StartsWithSegments("/health/live");
                    })
                    .AddHttpClientInstrumentation(instrumentation =>
                    {
                        instrumentation.RecordException = true;
                    });
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .SetResourceBuilder(resourceBuilder)
                    .AddMeter(OrtakareTelemetry.MeterName)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();
            });

        if (!string.IsNullOrWhiteSpace(options.OtlpEndpoint))
        {
            openTelemetry.UseOtlpExporter(exporter =>
            {
                exporter.Endpoint = new Uri(options.OtlpEndpoint);
            });
        }

        return services;
    }
}
