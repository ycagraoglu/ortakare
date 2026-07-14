using System.Text;
using Amazon.S3;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using Ortakare.Api.Common;
using Ortakare.Api.Extensions;
using Ortakare.Api.Features.Photos.UploadPhoto;
using Ortakare.Api.Infrastructure.Authentication;
using Ortakare.Api.Infrastructure.ObjectStorage;
using Ortakare.Api.Infrastructure.Persistence;
using Ortakare.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var message = string.Join(" ", context.ModelState.Values
                .SelectMany(x => x.Errors)
                .Select(x => x.ErrorMessage)
                .Where(x => !string.IsNullOrWhiteSpace(x)));

            var result = ApiResult.Failure(
                string.IsNullOrWhiteSpace(message) ? "Gönderilen bilgiler geçersiz." : message,
                StatusCodes.Status400BadRequest);

            return new BadRequestObjectResult(result);
        };
    });

builder.Services.AddOpenApi();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddFeatureHandlers();

builder.Services
    .AddOptions<PhotoUploadOptions>()
    .BindConfiguration(PhotoUploadOptions.SectionName)
    .Validate(x => x.MaxFileSizeBytes is > 0 and <= 100 * 1024 * 1024,
        "PhotoUpload:MaxFileSizeBytes must be between 1 byte and 100 MB.")
    .ValidateOnStart();

builder.Services.Configure<FormOptions>(options =>
{
    var configuredLimit = builder.Configuration
        .GetSection(PhotoUploadOptions.SectionName)
        .GetValue<long?>(nameof(PhotoUploadOptions.MaxFileSizeBytes))
        ?? 25 * 1024 * 1024;

    options.MultipartBodyLengthLimit = configuredLimit + 1024 * 1024;
});

builder.Services
    .AddOptions<ObjectStorageOptions>()
    .BindConfiguration(ObjectStorageOptions.SectionName);

builder.Services.AddSingleton<IAmazonS3>(serviceProvider =>
{
    var options = serviceProvider.GetRequiredService<IOptions<ObjectStorageOptions>>().Value;
    var config = new AmazonS3Config
    {
        ServiceURL = options.ServiceUrl,
        ForcePathStyle = true,
        AuthenticationRegion = "auto"
    };

    return new AmazonS3Client(options.AccessKey, options.SecretKey, config);
});

builder.Services.AddScoped<IObjectStorageService, R2ObjectStorageService>();

var connectionString = builder.Configuration.GetConnectionString("PostgreSql")
    ?? throw new InvalidOperationException("ConnectionStrings:PostgreSql is required.");

builder.Services.AddDbContext<OrtakareDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services
    .AddOptions<JwtOptions>()
    .BindConfiguration(JwtOptions.SectionName)
    .Validate(x => !string.IsNullOrWhiteSpace(x.Issuer), "Jwt:Issuer is required.")
    .Validate(x => !string.IsNullOrWhiteSpace(x.Audience), "Jwt:Audience is required.")
    .Validate(x => x.SigningKey.Length >= 32, "Jwt:SigningKey must contain at least 32 characters.")
    .Validate(x => x.AccessTokenMinutes is > 0 and <= 1440, "Jwt:AccessTokenMinutes must be between 1 and 1440.")
    .ValidateOnStart();

var jwtOptions = builder.Configuration
    .GetRequiredSection(JwtOptions.SectionName)
    .Get<JwtOptions>()
    ?? throw new InvalidOperationException("Jwt configuration is required.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
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

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program;