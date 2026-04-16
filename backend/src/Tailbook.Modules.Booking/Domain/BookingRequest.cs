namespace Tailbook.Modules.Booking.Domain;

public sealed class BookingRequest
{
    public Guid Id { get; set; }
    public Guid? ClientId { get; set; }
    public Guid PetId { get; set; }
    public Guid? RequestedByContactId { get; set; }
    public string Channel { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? PreferredTimeJson { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
