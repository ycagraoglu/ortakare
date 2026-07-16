using System.Globalization;
using System.Text;

namespace Ortakare.Api.Features.Notifications.GetMyNotifications;

internal readonly record struct NotificationCursor(DateTime CreatedAtUtc, Guid NotificationId)
{
    public string Encode()
    {
        var value = $"{CreatedAtUtc.Ticks.ToString(CultureInfo.InvariantCulture)}|{NotificationId:N}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
    }

    public static bool TryDecode(string? value, out NotificationCursor cursor)
    {
        cursor = default;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(value));
            var parts = decoded.Split('|', StringSplitOptions.TrimEntries);

            if (parts.Length != 2 ||
                !long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var ticks) ||
                !Guid.TryParseExact(parts[1], "N", out var notificationId) ||
                ticks < DateTime.MinValue.Ticks ||
                ticks > DateTime.MaxValue.Ticks)
            {
                return false;
            }

            cursor = new NotificationCursor(
                new DateTime(ticks, DateTimeKind.Utc),
                notificationId);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}