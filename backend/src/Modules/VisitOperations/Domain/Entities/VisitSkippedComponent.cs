using ErrorOr;

namespace Tailbook.Modules.VisitOperations.Domain.Entities;

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
    public DateTimeOffset RecordedAt { get; private set; }

    internal static ErrorOr<VisitSkippedComponent> Create(
        Guid id,
        Guid visitExecutionItemId,
        VisitSkippedComponentDraft component,
        Guid? recordedByUserId,
        DateTimeOffset recordedAt)
    {
        List<Error> errors = [];

        if (component is null)
        {
            return Error.Validation("VisitOperations.SkippedComponentRequired", "Skipped component is required.");
        }

        if (id == Guid.Empty)
        {
            errors.Add(Error.Validation("VisitOperations.SkippedComponentIdRequired", "Skipped component id is required."));
        }

        if (visitExecutionItemId == Guid.Empty)
        {
            errors.Add(Error.Validation("VisitOperations.SkippedComponentExecutionItemRequired", "Skipped component must belong to a visit execution item."));
        }

        if (component.OfferVersionComponentId == Guid.Empty)
        {
            errors.Add(Error.Validation("VisitOperations.SkippedComponentOfferVersionComponentRequired", "Skipped component must reference an offer version component."));
        }

        if (component.ProcedureId == Guid.Empty)
        {
            errors.Add(Error.Validation("VisitOperations.SkippedComponentProcedureRequired", "Skipped component must reference a procedure."));
        }

        if (string.IsNullOrWhiteSpace(component.ProcedureCodeSnapshot))
        {
            errors.Add(Error.Validation("VisitOperations.SkippedComponentProcedureCodeRequired", "Skipped component must include a procedure code snapshot."));
        }

        if (string.IsNullOrWhiteSpace(component.ProcedureNameSnapshot))
        {
            errors.Add(Error.Validation("VisitOperations.SkippedComponentProcedureNameRequired", "Skipped component must include a procedure name snapshot."));
        }

        var omissionReasonCode = NormalizeRequiredCode(component.OmissionReasonCode, "Omission reason code is required.");
        if (omissionReasonCode.IsError)
        {
            errors.AddRange(omissionReasonCode.Errors);
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        return new VisitSkippedComponent
        {
            Id = id,
            VisitExecutionItemId = visitExecutionItemId,
            OfferVersionComponentId = component.OfferVersionComponentId,
            ProcedureId = component.ProcedureId,
            ProcedureCodeSnapshot = component.ProcedureCodeSnapshot.Trim(),
            ProcedureNameSnapshot = component.ProcedureNameSnapshot.Trim(),
            OmissionReasonCode = omissionReasonCode.Value,
            Note = NormalizeOptional(component.Note),
            RecordedByUserId = recordedByUserId,
            RecordedAt = recordedAt.ToUniversalTime()
        };
    }

    private static ErrorOr<string> NormalizeRequiredCode(string? value, string message)
    {
        var normalized = NormalizeOptional(value);
        if (normalized is null)
        {
            return Error.Validation("VisitOperations.RequiredCodeMissing", message);
        }

        return normalized.ToUpperInvariant();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
