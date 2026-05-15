namespace Tailbook.Modules.Booking.Domain.Aggregates;

public sealed class BookingRequest
{
    public Guid Id { get; set; }
    public Guid? ClientId { get; set; }
    public Guid? PetId { get; set; }
    public Guid? RequestedByContactId { get; set; }
    public Guid? PreferredGroomerId { get; set; }
    public string Channel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? SelectionMode { get; set; }
    public string? GuestIntakeJson { get; set; }
    public string? PreferredTimeJson { get; set; }
    public string? Notes { get; set; }
    public int VersionNo { get; set; } = 1;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
