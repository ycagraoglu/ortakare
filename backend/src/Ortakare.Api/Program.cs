using System.Text;
using System.Threading.RateLimiting;
using Amazon.S3;
using FluentValidation;
using FluentValidation.AspNetCore;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Ortakare.Api.Common;
using Ortakare.Api.Extensions;
using Ortakare.Api.Features.GalleryExports;
using Ortakare.Api.Features.Notifications.Streaming;
using Ortakare.Api.Features.Photos.UploadPhoto;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.BackgroundJobs;
using Ortakare.Api.Infrastructure.Cors;
using Ortakare.Api.Infrastructure.HealthChecks;
using Ortakare.Api.Infrastructure.ObjectStorage;
using Ortakare.Api.Infrastructure.Persistence;
using Ortakare.Api.Infrastructure.Proxy;
using Ortakare.Api.Infrastructure.RateLimiting;
using Ortakare.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var message = string.Join(" ", context.ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage).Where(x => !string.IsNullOrWhiteSpace(x)));
        var result = ApiResult.Failure(string.IsNullOrWhiteSpace(message) ? "Gönderilen bilgiler geçersiz." : message, StatusCodes.Status400BadRequest);
        return new BadRequestObjectResult(result);
    };
});

builder.Services.AddOpenApi();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddFeatureHandlers();
builder.Services.AddTrustedForwardedHeaders(builder.Configuration);
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
});

builder.Services.AddOptions<CorsOptions>()
    .BindConfiguration(CorsOptions.SectionName)
    .Validate(x => x.AllowedOrigins.Length > 0, "Cors:AllowedOrigins must contain at least one origin.")
    .Validate(x => x.AllowedOrigins.All(IsValidCorsOrigin), "Cors:AllowedOrigins must contain valid absolute HTTP or HTTPS origins without paths.")
    .ValidateOnStart();

var corsOptions = builder.Configuration.GetRequiredSection(CorsOptions.SectionName).Get<CorsOptions>()
    ?? throw new InvalidOperationException("Cors configuration is required.");

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicies.Pwa, policy =>
    {
        policy.WithOrigins(corsOptions.AllowedOrigins)
            .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
            .WithHeaders("Authorization", "Content-Type", "X-Participant-Token", "X-Client-Upload-Id");
    });
});

builder.Services.AddOptions<RateLimitingOptions>()
    .BindConfiguration(RateLimitingOptions.SectionName)
    .Validate(x => x.AuthPermitLimit > 0, "RateLimiting:AuthPermitLimit must be greater than zero.")
    .Validate(x => x.PublicPermitLimit > 0, "RateLimiting:PublicPermitLimit must be greater than zero.")
    .Validate(x => x.UploadPermitLimit > 0, "RateLimiting:UploadPermitLimit must be greater than zero.")
    .Validate(x => x.OwnerPermitLimit > 0, "RateLimiting:OwnerPermitLimit must be greater than zero.")
    .Validate(x => x.WindowSeconds is > 0 and <= 3600, "RateLimiting:WindowSeconds must be between 1 and 3600.")
    .ValidateOnStart();

var rateLimitingOptions = builder.Configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>() ?? new RateLimitingOptions();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsJsonAsync(ApiResult.Failure("Çok fazla istek gönderildi. Lütfen kısa bir süre sonra tekrar deneyin.", StatusCodes.Status429TooManyRequests), cancellationToken);
    };

    AddFixedWindowPolicy(options, RateLimitingPolicies.Auth, rateLimitingOptions.AuthPermitLimit, rateLimitingOptions.WindowSeconds);
    AddFixedWindowPolicy(options, RateLimitingPolicies.Public, rateLimitingOptions.PublicPermitLimit, rateLimitingOptions.WindowSeconds);
    AddFixedWindowPolicy(options, RateLimitingPolicies.Upload, rateLimitingOptions.UploadPermitLimit, rateLimitingOptions.WindowSeconds);
    AddFixedWindowPolicy(options, RateLimitingPolicies.Owner, rateLimitingOptions.OwnerPermitLimit, rateLimitingOptions.WindowSeconds);
});

builder.Services.AddOptions<NotificationStreamOptions>()
    .BindConfiguration(NotificationStreamOptions.SectionName)
    .Validate(x => x.TokenLifetimeSeconds is >= 15 and <= 300, "NotificationStream:TokenLifetimeSeconds must be between 15 and 300.")
    .ValidateOnStart();

builder.Services.AddOptions<PhotoUploadOptions>()
    .BindConfiguration(PhotoUploadOptions.SectionName)
    .Validate(x => x.MaxFileSizeBytes is > 0 and <= 100 * 1024 * 1024, "PhotoUpload:MaxFileSizeBytes must be between 1 byte and 100 MB.")
    .ValidateOnStart();

builder.Services.Configure<FormOptions>(options =>
{
    var configuredLimit = builder.Configuration.GetSection(PhotoUploadOptions.SectionName).GetValue<long?>(nameof(PhotoUploadOptions.MaxFileSizeBytes)) ?? 25 * 1024 * 1024;
    options.MultipartBodyLengthLimit = configuredLimit + 1024 * 1024;
});

builder.Services.AddOptions<ObjectStorageOptions>().BindConfiguration(ObjectStorageOptions.SectionName);
builder.Services.AddSingleton<IAmazonS3>(serviceProvider =>
{
    var options = serviceProvider.GetRequiredService<IOptions<ObjectStorageOptions>>().Value;
    var config = new AmazonS3Config { ServiceURL = options.ServiceUrl, ForcePathStyle = true, AuthenticationRegion = "auto" };
    return new AmazonS3Client(options.AccessKey, options.SecretKey, config);
});
builder.Services.AddScoped<IObjectStorageService, R2ObjectStorageService>();

var connectionString = builder.Configuration.GetConnectionString("PostgreSql") ?? throw new InvalidOperationException("ConnectionStrings:PostgreSql is required.");
builder.Services.AddDbContext<OrtakareDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddHealthChecks()
    .AddCheck<PostgreSqlHealthCheck>("postgresql", tags: ["ready"])
    .AddCheck<R2HealthCheck>("r2", tags: ["ready"]);

var hangfireEnabled = builder.Configuration.GetValue("Hangfire:Enabled", true);
var hangfireDashboardOptions = builder.Configuration.GetSection(HangfireDashboardOptions.SectionName).Get<HangfireDashboardOptions>() ?? new HangfireDashboardOptions();
var backgroundJobShutdownOptions = builder.Configuration.GetSection(BackgroundJobShutdownOptions.SectionName).Get<BackgroundJobShutdownOptions>() ?? new BackgroundJobShutdownOptions();

builder.Services.AddOptions<HangfireDashboardOptions>()
    .BindConfiguration(HangfireDashboardOptions.SectionName)
    .Validate(x => !x.Enabled || IsValidDashboardPath(x.Path), "Hangfire:Dashboard:Path must be a local absolute path.")
    .Validate(x => !x.Enabled || x.Username.Length >= 8, "Hangfire:Dashboard:Username must contain at least 8 characters when dashboard is enabled.")
    .Validate(x => !x.Enabled || x.Password.Length >= 24, "Hangfire:Dashboard:Password must contain at least 24 characters when dashboard is enabled.")
    .ValidateOnStart();

builder.Services.AddOptions<BackgroundJobShutdownOptions>()
    .BindConfiguration(BackgroundJobShutdownOptions.SectionName)
    .Validate(x => x.HostShutdownSeconds is > 0 and <= 600, "Hangfire:Shutdown:HostShutdownSeconds must be between 1 and 600.")
    .Validate(x => x.StopSeconds is > 0 and <= 600, "Hangfire:Shutdown:StopSeconds must be between 1 and 600.")
    .Validate(x => x.ShutdownSeconds is > 0 and <= 600, "Hangfire:Shutdown:ShutdownSeconds must be between 1 and 600.")
    .Validate(x => x.CancellationCheckSeconds is > 0 and <= 30, "Hangfire:Shutdown:CancellationCheckSeconds must be between 1 and 30.")
    .Validate(x => x.HostShutdownSeconds >= x.ShutdownSeconds, "Host shutdown timeout must be greater than or equal to Hangfire shutdown timeout.")
    .ValidateOnStart();

builder.Services.Configure<HostOptions>(options =>
    options.ShutdownTimeout = TimeSpan.FromSeconds(backgroundJobShutdownOptions.HostShutdownSeconds));

if (hangfireEnabled)
{
    builder.Services.AddHangfire(configuration => configuration.UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString)));
    builder.Services.AddHangfireServer(options =>
    {
        options.WorkerCount = builder.Configuration.GetValue("Hangfire:WorkerCount", 2);
        options.StopTimeout = TimeSpan.FromSeconds(backgroundJobShutdownOptions.StopSeconds);
        options.ShutdownTimeout = TimeSpan.FromSeconds(backgroundJobShutdownOptions.ShutdownSeconds);
        options.CancellationCheckInterval = TimeSpan.FromSeconds(backgroundJobShutdownOptions.CancellationCheckSeconds);
    });
    builder.Services.AddScoped<IGalleryExportJobScheduler, HangfireGalleryExportJobScheduler>();
}

builder.Services.AddOptions<JwtOptions>()
    .BindConfiguration(JwtOptions.SectionName)
    .Validate(x => !string.IsNullOrWhiteSpace(x.Issuer), "Jwt:Issuer is required.")
    .Validate(x => !string.IsNullOrWhiteSpace(x.Audience), "Jwt:Audience is required.")
    .Validate(x => x.SigningKey.Length >= 32, "Jwt:SigningKey must contain at least 32 characters.")
    .Validate(x => x.AccessTokenMinutes is > 0 and <= 1440, "Jwt:AccessTokenMinutes must be between 1 and 1440.")
    .ValidateOnStart();

var jwtOptions = builder.Configuration.GetRequiredSection(JwtOptions.SectionName).Get<JwtOptions>() ?? throw new InvalidOperationException("Jwt configuration is required.");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwtOptions.Issuer,
        ValidateAudience = true,
        ValidAudience = jwtOptions.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30)
    };
});

builder.Services.AddAuthorization();
var app = builder.Build();
app.UseForwardedHeaders();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
if (!app.Environment.IsDevelopment()) app.UseHsts();
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.UseHttpsRedirection();
app.UseCors(CorsPolicies.Pwa);
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseAuthorization();

if (hangfireEnabled && hangfireDashboardOptions.Enabled)
{
    app.UseHangfireDashboard(hangfireDashboardOptions.Path, new DashboardOptions
    {
        Authorization = [new HangfireDashboardAuthorizationFilter(hangfireDashboardOptions)],
        DashboardTitle = "Ortakare Operations Jobs",
        IgnoreAntiforgeryToken = false
    });
}

app.MapHealthChecks("/health/application", new HealthCheckOptions
{
    Predicate = _ => false,
    ResponseWriter = HealthCheckResponseWriter.WriteAsync
});
app.MapHealthChecks("/health/dependencies", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready"),
    ResponseWriter = HealthCheckResponseWriter.WriteAsync
});
app.MapControllers();
app.Run();

static void AddFixedWindowPolicy(RateLimiterOptions options, string policyName, int permitLimit, int windowSeconds)
{
    options.AddPolicy(policyName, httpContext =>
    {
        var partitionKey = httpContext.User.Identity?.IsAuthenticated == true
            ? $"user:{httpContext.User.FindFirst("sub")?.Value ?? "unknown"}"
            : $"ip:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";

        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = TimeSpan.FromSeconds(windowSeconds),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    });
}

static bool IsValidCorsOrigin(string origin)
{
    if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)) return false;
    if (uri.Scheme is not ("http" or "https")) return false;
    return uri.AbsolutePath == "/" && string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment);
}

static bool IsValidDashboardPath(string path)
{
    return path.StartsWith('/', StringComparison.Ordinal)
        && !path.StartsWith("//", StringComparison.Ordinal)
        && !path.Contains('?')
        && !path.Contains('#');
}

public partial class Program;
