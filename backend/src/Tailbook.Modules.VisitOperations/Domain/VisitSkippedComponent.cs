namespace Tailbook.Modules.VisitOperations.Domain;

public sealed class VisitSkippedComponent
{
    private VisitSkippedComponent()
    {
    }

    public Guid Id { get; private set; }
    public Guid VisitExecutionItemId { get; private set; }
    public Guid OfferVersionComponentId { get; private set; }
    public Guid ProcedureId { get; private set; }
    public string ProcedureCodeSnapshot { get; private set; } = string.Empty;
    public string ProcedureNameSnapshot { get; private set; } = string.Empty;
    public string OmissionReasonCode { get; private set; } = string.Empty;
    public string? Note { get; private set; }
    public Guid? RecordedByUserId { get; private set; }
    public DateTime RecordedAtUtc { get; private set; }

    internal static VisitSkippedComponent Create(
        Guid id,
        Guid visitExecutionItemId,
        VisitSkippedComponentDraft component,
        Guid? recordedByUserId,
        DateTime recordedAtUtc)
    {
        if (component is null)
        {
            throw new InvalidOperationException("Skipped component is required.");
        }

        if (id == Guid.Empty)
        {
            throw new InvalidOperationException("Skipped component id is required.");
        }

        if (visitExecutionItemId == Guid.Empty)
        {
            throw new InvalidOperationException("Skipped component must belong to a visit execution item.");
        }

        if (component.OfferVersionComponentId == Guid.Empty)
        {
            throw new InvalidOperationException("Skipped component must reference an offer version component.");
        }

        if (component.ProcedureId == Guid.Empty)
        {
            throw new InvalidOperationException("Skipped component must reference a procedure.");
        }

        if (string.IsNullOrWhiteSpace(component.ProcedureCodeSnapshot))
        {
            throw new InvalidOperationException("Skipped component must include a procedure code snapshot.");
        }

        if (string.IsNullOrWhiteSpace(component.ProcedureNameSnapshot))
        {
            throw new InvalidOperationException("Skipped component must include a procedure name snapshot.");
        }

        return new VisitSkippedComponent
        {
            Id = id,
            VisitExecutionItemId = visitExecutionItemId,
            OfferVersionComponentId = component.OfferVersionComponentId,
            ProcedureId = component.ProcedureId,
            ProcedureCodeSnapshot = component.ProcedureCodeSnapshot.Trim(),
            ProcedureNameSnapshot = component.ProcedureNameSnapshot.Trim(),
            OmissionReasonCode = NormalizeRequiredCode(component.OmissionReasonCode, "Omission reason code is required."),
            Note = NormalizeOptional(component.Note),
            RecordedByUserId = recordedByUserId,
            RecordedAtUtc = DateTime.SpecifyKind(recordedAtUtc, DateTimeKind.Utc)
        };
    }

    private static string NormalizeRequiredCode(string? value, string message)
    {
        var normalized = NormalizeOptional(value);
        if (normalized is null)
        {
            throw new InvalidOperationException(message);
        }

        return normalized.ToUpperInvariant();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
