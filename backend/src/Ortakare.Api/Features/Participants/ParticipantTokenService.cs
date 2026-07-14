using System.Security.Cryptography;
using System.Text;

namespace Ortakare.Api.Features.Participants;

public sealed class ParticipantTokenService
{
    public ParticipantToken Create()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        var value = Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        return new ParticipantToken(value, Hash(value));
    }

    public string Hash(string value)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash);
    }
}

public sealed record ParticipantToken(string Value, string Hash);
