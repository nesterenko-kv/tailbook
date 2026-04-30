using Microsoft.Extensions.Options;

namespace Tailbook.Modules.Staff.Infrastructure.Services;

public sealed class SalonTimeZoneProvider(IOptions<StaffSchedulingOptions> options)
{
    public TimeZoneInfo GetTimeZone()
    {
        var configuredId = options.Value.TimeZoneId;
        var candidates = new[] { configuredId, "Europe/Kyiv", "FLE Standard Time", "UTC" }
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in candidates)
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(candidate);
            }
            catch
            {
            }
        }

        return TimeZoneInfo.Utc;
    }
}
