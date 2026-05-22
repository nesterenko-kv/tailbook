namespace Tailbook.Modules.Booking.Api.Public;

public sealed class GetPublicBookingRequestStatusResponse
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? AppointmentId { get; set; }
}
