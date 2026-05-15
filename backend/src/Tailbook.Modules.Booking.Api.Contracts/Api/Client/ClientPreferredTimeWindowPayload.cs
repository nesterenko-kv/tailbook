namespace Tailbook.Modules.Booking.Api.Client;

public sealed class ClientPreferredTimeWindowPayload
{
    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset EndAt { get; set; }
    public string? Label { get; set; }
}