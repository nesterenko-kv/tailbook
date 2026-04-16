namespace Tailbook.Modules.VisitOperations.Domain;

public sealed class Visit
{
    public Guid Id { get; set; }
    public Guid AppointmentId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CheckedInAtUtc { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
