using ErrorOr;

namespace Tailbook.Modules.VisitOperations.Domain.Entities;

public sealed class VisitPerformedProcedure
{
    private VisitPerformedProcedure()
    {
    }

    public Guid Id { get; private set; }
    public Guid VisitExecutionItemId { get; private set; }
    public Guid ProcedureId { get; private set; }
    public string ProcedureCodeSnapshot { get; private set; } = string.Empty;
    public string ProcedureNameSnapshot { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;
    public string? Note { get; private set; }
    public Guid? RecordedByUserId { get; private set; }
    public DateTimeOffset RecordedAt { get; private set; }

    internal static ErrorOr<VisitPerformedProcedure> Create(
        Guid id,
        Guid visitExecutionItemId,
        VisitPerformedProcedureDraft procedure,
        Guid? recordedByUserId,
        DateTimeOffset recordedAt)
    {
        List<Error> errors = [];

        if (procedure is null)
        {
            return Error.Validation("VisitOperations.PerformedProcedureRequired", "Performed procedure is required.");
        }

        if (id == Guid.Empty)
        {
            errors.Add(Error.Validation("VisitOperations.PerformedProcedureIdRequired", "Performed procedure id is required."));
        }

        if (visitExecutionItemId == Guid.Empty)
        {
            errors.Add(Error.Validation("VisitOperations.PerformedProcedureExecutionItemRequired", "Performed procedure must belong to a visit execution item."));
        }

        if (procedure.ProcedureId == Guid.Empty)
        {
            errors.Add(Error.Validation("VisitOperations.PerformedProcedureProcedureRequired", "Performed procedure must reference a procedure."));
        }

        if (string.IsNullOrWhiteSpace(procedure.ProcedureCodeSnapshot))
        {
            errors.Add(Error.Validation("VisitOperations.PerformedProcedureCodeRequired", "Performed procedure must include a procedure code snapshot."));
        }

        if (string.IsNullOrWhiteSpace(procedure.ProcedureNameSnapshot))
        {
            errors.Add(Error.Validation("VisitOperations.PerformedProcedureNameRequired", "Performed procedure must include a procedure name snapshot."));
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        return new VisitPerformedProcedure
        {
            Id = id,
            VisitExecutionItemId = visitExecutionItemId,
            ProcedureId = procedure.ProcedureId,
            ProcedureCodeSnapshot = procedure.ProcedureCodeSnapshot.Trim(),
            ProcedureNameSnapshot = procedure.ProcedureNameSnapshot.Trim(),
            Status = ProcedureExecutionStatusCodes.Performed,
            Note = NormalizeOptional(procedure.Note),
            RecordedByUserId = recordedByUserId,
            RecordedAt = recordedAt.ToUniversalTime()
        };
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
