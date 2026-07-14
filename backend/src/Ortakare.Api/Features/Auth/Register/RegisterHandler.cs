using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Ortakare.Api.Common;
using Ortakare.Api.Features.Users;
using Ortakare.Api.Infrastructure.Persistence;

namespace Ortakare.Api.Features.Auth.Register;

public sealed class RegisterHandler(
    OrtakareDbContext dbContext,
    IPasswordHasher<User> passwordHasher,
    TimeProvider timeProvider)
{
    public async Task<ApiResult<RegisterResponse>> HandleAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var displayName = request.DisplayName.Trim();
        var email = request.Email.Trim();
        var normalizedEmail = email.ToUpperInvariant();

        var emailExists = await dbContext.Users
            .AsNoTracking()
            .AnyAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken);

        if (emailExists)
        {
            return ApiResult<RegisterResponse>.Failure(
                "Bu e-posta adresiyle daha önce kayıt oluşturulmuş.",
                StatusCodes.Status409Conflict);
        }

        var user = new User
        {
            Id = Guid.CreateVersion7(),
            DisplayName = displayName,
            Email = email,
            NormalizedEmail = normalizedEmail,
            CreatedAtUtc = timeProvider.GetUtcNow().UtcDateTime
        };

        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new RegisterResponse(
            user.Id,
            user.DisplayName,
            user.Email,
            user.CreatedAtUtc);

        return ApiResult<RegisterResponse>.Success(
            response,
            "Kullanıcı kaydı oluşturuldu.",
            StatusCodes.Status201Created);
    }
}
