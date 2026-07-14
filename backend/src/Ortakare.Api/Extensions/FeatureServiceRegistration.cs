using Microsoft.AspNetCore.Identity;
using Ortakare.Api.Features.Auth.Login;
using Ortakare.Api.Features.Auth.Register;
using Ortakare.Api.Features.System.Health;
using Ortakare.Api.Features.Users;
using Ortakare.Api.Infrastructure.Authentication;

namespace Ortakare.Api.Extensions;

public static class FeatureServiceRegistration
{
    public static IServiceCollection AddFeatureHandlers(this IServiceCollection services)
    {
        services.AddScoped<GetHealthHandler>();
        services.AddScoped<RegisterHandler>();
        services.AddScoped<LoginHandler>();
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<IAccessTokenService, JwtAccessTokenService>();

        return services;
    }
}
