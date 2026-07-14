using Microsoft.AspNetCore.Identity;
using Ortakare.Api.Features.Auth.Login;
using Ortakare.Api.Features.Auth.Refresh;
using Ortakare.Api.Features.Auth.Register;
using Ortakare.Api.Features.Events.CloseEvent;
using Ortakare.Api.Features.Events.CreateEvent;
using Ortakare.Api.Features.Events.GetEvent;
using Ortakare.Api.Features.Events.GetMyEvents;
using Ortakare.Api.Features.Events.UpdateEvent;
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
        services.AddScoped<RefreshHandler>();
        services.AddScoped<CreateEventHandler>();
        services.AddScoped<GetMyEventsHandler>();
        services.AddScoped<GetEventHandler>();
        services.AddScoped<UpdateEventHandler>();
        services.AddScoped<CloseEventHandler>();

        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddSingleton<IAccessTokenService, JwtAccessTokenService>();
        services.AddSingleton<IRefreshTokenService, RefreshTokenService>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();

        return services;
    }
}
