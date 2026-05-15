namespace Tailbook.Modules.Staff.Api.Admin.CheckAvailability;

public sealed class CheckAvailabilityResponse
{
    public bool IsAvailable { get; set; }
    public DateTimeOffset EndAt { get; set; }
    public int CheckedReservedMinutes { get; set; }
    public string[] Reasons { get; set; } = [];
}