namespace Tailbook.Modules.Booking.Domain;

public sealed class Appointment
{
    public Guid Id { get; set; }
    public Guid? BookingRequestId { get; set; }
    public Guid PetId { get; set; }
    public Guid GroomerId { get; set; }
    public DateTime StartAtUtc { get; set; }
    public DateTime EndAtUtc { get; set; }
    public string Status { get; set; } = string.Empty;
    public int VersionNo { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public string? CancellationReasonCode { get; set; }
    public string? CancellationNotes { get; set; }
    public DateTime? CancelledAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
