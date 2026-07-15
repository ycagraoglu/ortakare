using Microsoft.AspNetCore.Identity;
using Ortakare.Api.Features.Auth.Login;
using Ortakare.Api.Features.Auth.Logout;
using Ortakare.Api.Features.Auth.Refresh;
using Ortakare.Api.Features.Auth.Register;
using Ortakare.Api.Features.Events.CloseEvent;
using Ortakare.Api.Features.Events.CreateEvent;
using Ortakare.Api.Features.Events.GetEvent;
using Ortakare.Api.Features.Events.GetMyEvents;
using Ortakare.Api.Features.Events.RegenerateGalleryToken;
using Ortakare.Api.Features.Events.ReopenEvent;
using Ortakare.Api.Features.Events.UpdateEvent;
using Ortakare.Api.Features.GalleryExports.CreateGalleryExport;
using Ortakare.Api.Features.GalleryExports.GetEventExports;
using Ortakare.Api.Features.GalleryExports.GetGalleryExport;
using Ortakare.Api.Features.Participants;
using Ortakare.Api.Features.Participants.GetEventParticipants;
using Ortakare.Api.Features.Participants.JoinEvent;
using Ortakare.Api.Features.Participants.UpdateParticipantDisplayName;
using Ortakare.Api.Features.Photos.DeleteGuestPhoto;
using Ortakare.Api.Features.Photos.DeleteOwnerPhoto;
using Ortakare.Api.Features.Photos.GetEventPhotos;
using Ortakare.Api.Features.Photos.UploadPhoto;
using Ortakare.Api.Features.PublicEvents.GetPublicEvent;
using Ortakare.Api.Features.System.Health;
using Ortakare.Api.Features.Users;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.BackgroundJobs;

namespace Ortakare.Api.Extensions;

public static class FeatureServiceRegistration
{
    public static IServiceCollection AddFeatureHandlers(this IServiceCollection services)
    {
        services.AddScoped<GetHealthHandler>();
        services.AddScoped<RegisterHandler>();
        services.AddScoped<LoginHandler>();
        services.AddScoped<RefreshHandler>();
        services.AddScoped<LogoutHandler>();
        services.AddScoped<CreateEventHandler>();
        services.AddScoped<GetMyEventsHandler>();
        services.AddScoped<GetEventHandler>();
        services.AddScoped<UpdateEventHandler>();
        services.AddScoped<CloseEventHandler>();
        services.AddScoped<ReopenEventHandler>();
        services.AddScoped<RegenerateGalleryTokenHandler>();
        services.AddScoped<GetPublicEventHandler>();
        services.AddScoped<JoinEventHandler>();
        services.AddScoped<GetEventParticipantsHandler>();
        services.AddScoped<UpdateParticipantDisplayNameHandler>();
        services.AddScoped<UploadPhotoHandler>();
        services.AddScoped<GetEventPhotosHandler>();
        services.AddScoped<DeleteOwnerPhotoHandler>();
        services.AddScoped<DeleteGuestPhotoHandler>();
        services.AddScoped<CreateGalleryExportHandler>();
        services.AddScoped<GetGalleryExportHandler>();
        services.AddScoped<GetEventExportsHandler>();
        services.AddScoped<BuildGalleryExportJob>();

        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddSingleton<IAccessTokenService, JwtAccessTokenService>();
        services.AddSingleton<IRefreshTokenService, RefreshTokenService>();
        services.AddSingleton<ParticipantTokenService>();
        services.AddSingleton<ImageFileInspector>();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();

        return services;
    }
}
