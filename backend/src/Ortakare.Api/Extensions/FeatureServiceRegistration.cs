using Ortakare.Api.Features.System.Health;

namespace Ortakare.Api.Extensions;

public static class FeatureServiceRegistration
{
    public static IServiceCollection AddFeatureHandlers(this IServiceCollection services)
    {
        services.AddScoped<GetHealthHandler>();
        return services;
    }
}
