namespace Tailbook.Modules.Booking.Api.Public;

public sealed class PublicPreferredTimePayload
{
    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset EndAt { get; set; }
    public string? Label { get; set; }
}