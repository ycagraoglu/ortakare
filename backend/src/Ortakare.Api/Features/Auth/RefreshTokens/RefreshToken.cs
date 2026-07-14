namespace Ortakare.Api.Features.Auth.RefreshTokens;

public sealed class RefreshToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public Guid? ReplacedByTokenId { get; set; }

    public bool IsActive(DateTime utcNow) =>
        UsedAtUtc is null && RevokedAtUtc is null && ExpiresAtUtc > utcNow;
}