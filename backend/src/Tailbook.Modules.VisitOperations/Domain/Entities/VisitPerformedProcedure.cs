using Tailbook.Modules.VisitOperations.Contracts;

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
    public DateTime RecordedAtUtc { get; private set; }

    internal static VisitPerformedProcedure Create(
        Guid id,
        Guid visitExecutionItemId,
        VisitPerformedProcedureDraft procedure,
        Guid? recordedByUserId,
        DateTime recordedAtUtc)
    {
        if (procedure is null)
        {
            throw new InvalidOperationException("Performed procedure is required.");
        }

        if (id == Guid.Empty)
        {
            throw new InvalidOperationException("Performed procedure id is required.");
        }

        if (visitExecutionItemId == Guid.Empty)
        {
            throw new InvalidOperationException("Performed procedure must belong to a visit execution item.");
        }

        if (procedure.ProcedureId == Guid.Empty)
        {
            throw new InvalidOperationException("Performed procedure must reference a procedure.");
        }

        if (string.IsNullOrWhiteSpace(procedure.ProcedureCodeSnapshot))
        {
            throw new InvalidOperationException("Performed procedure must include a procedure code snapshot.");
        }

        if (string.IsNullOrWhiteSpace(procedure.ProcedureNameSnapshot))
        {
            throw new InvalidOperationException("Performed procedure must include a procedure name snapshot.");
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
            RecordedAtUtc = DateTime.SpecifyKind(recordedAtUtc, DateTimeKind.Utc)
        };
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
