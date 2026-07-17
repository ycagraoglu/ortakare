using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace Ortakare.Api.Features.Notifications.Streaming;

public interface INotificationStreamTokenService
{
    NotificationStreamTokenIssueResult Issue(Guid ownerUserId);
    NotificationStreamTokenConsumeResult? Consume(string token);
}

public sealed record NotificationStreamTokenIssueResult(
    string Token,
    DateTime ExpiresAtUtc);

public sealed record NotificationStreamTokenConsumeResult(
    Guid OwnerUserId,
    DateTime ExpiresAtUtc);

public sealed class NotificationStreamTokenService(
    IOptions<NotificationStreamOptions> options,
    TimeProvider timeProvider) : INotificationStreamTokenService
{
    private readonly ConcurrentDictionary<string, TokenEntry> _tokens = new(StringComparer.Ordinal);
    private readonly TimeSpan _tokenLifetime = TimeSpan.FromSeconds(options.Value.TokenLifetimeSeconds);

    public NotificationStreamTokenIssueResult Issue(Guid ownerUserId)
    {
        var now = timeProvider.GetUtcNow();
        RemoveExpiredTokens(now);

        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        var tokenHash = Hash(rawToken);
        var expiresAtUtc = now.Add(_tokenLifetime).UtcDateTime;

        _tokens[tokenHash] = new TokenEntry(ownerUserId, expiresAtUtc);

        return new NotificationStreamTokenIssueResult(rawToken, expiresAtUtc);
    }

    public NotificationStreamTokenConsumeResult? Consume(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var now = timeProvider.GetUtcNow();
        var tokenHash = Hash(token);

        if (!_tokens.TryRemove(tokenHash, out var entry))
        {
            return null;
        }

        if (entry.ExpiresAtUtc <= now.UtcDateTime)
        {
            return null;
        }

        return new NotificationStreamTokenConsumeResult(entry.OwnerUserId, entry.ExpiresAtUtc);
    }

    private void RemoveExpiredTokens(DateTimeOffset now)
    {
        foreach (var pair in _tokens)
        {
            if (pair.Value.ExpiresAtUtc <= now.UtcDateTime)
            {
                _tokens.TryRemove(pair.Key, out _);
            }
        }
    }

    private static string Hash(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }

    private sealed record TokenEntry(Guid OwnerUserId, DateTime ExpiresAtUtc);
}
