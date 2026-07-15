using System.Security.Cryptography;
using System.Text;
using Hangfire.Dashboard;

namespace Ortakare.Api.Infrastructure.BackgroundJobs;

public sealed class HangfireDashboardAuthorizationFilter(HangfireDashboardOptions options) : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        if (!httpContext.Request.IsHttps)
        {
            return false;
        }

        var authorization = httpContext.Request.Headers.Authorization.ToString();
        if (!authorization.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
        {
            Challenge(httpContext);
            return false;
        }

        try
        {
            var encodedCredentials = authorization["Basic ".Length..].Trim();
            var credentials = Encoding.UTF8.GetString(Convert.FromBase64String(encodedCredentials));
            var separatorIndex = credentials.IndexOf(':');

            if (separatorIndex <= 0)
            {
                Challenge(httpContext);
                return false;
            }

            var username = credentials[..separatorIndex];
            var password = credentials[(separatorIndex + 1)..];
            var authorized = FixedTimeEquals(username, options.Username) && FixedTimeEquals(password, options.Password);

            if (!authorized)
            {
                Challenge(httpContext);
            }

            return authorized;
        }
        catch (FormatException)
        {
            Challenge(httpContext);
            return false;
        }
    }

    private static bool FixedTimeEquals(string actual, string expected)
    {
        var actualHash = SHA256.HashData(Encoding.UTF8.GetBytes(actual));
        var expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(expected));
        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }

    private static void Challenge(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.Headers.WWWAuthenticate = "Basic realm=\"Ortakare Operations\", charset=\"UTF-8\"";
    }
}
