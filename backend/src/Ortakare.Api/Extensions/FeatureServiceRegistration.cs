using Microsoft.AspNetCore.Identity;
using Ortakare.Api.Features.Auth.Register;
using Ortakare.Api.Features.System.Health;
using Ortakare.Api.Features.Users;

namespace Ortakare.Api.Extensions;

public static class FeatureServiceRegistration
{
    public static IServiceCollection AddFeatureHandlers(this IServiceCollection services)
    {
        services.AddScoped<GetHealthHandler>();
        services.AddScoped<RegisterHandler>();
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

        return services;
    }
}
