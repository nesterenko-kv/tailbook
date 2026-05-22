namespace Tailbook.Modules.Booking.Api.Admin.CreateBookingRequest;

public sealed class PreferredTimeWindowPayload
{
    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset EndAt { get; set; }
    public string? Label { get; set; }
}