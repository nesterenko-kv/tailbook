namespace Tailbook.Modules.Booking.Api.Client;

public sealed class ClientPreferredTimeWindowPayload
{
    public DateTime StartAtUtc { get; set; }
    public DateTime EndAtUtc { get; set; }
    public string? Label { get; set; }
}