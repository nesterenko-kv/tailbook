using Tailbook.Modules.VisitOperations.Contracts;

namespace Tailbook.Modules.VisitOperations.Domain;

public sealed class Visit
{
    private readonly List<VisitExecutionItem> _executionItems = [];
    private readonly List<VisitPriceAdjustment> _priceAdjustments = [];

    private Visit()
    {
    }

    public Guid Id { get; private set; }
    public Guid AppointmentId { get; private set; }
    public string Status { get; private set; } = string.Empty;
    public DateTime CheckedInAtUtc { get; private set; }
    public DateTime? StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public DateTime? ClosedAtUtc { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public IReadOnlyCollection<VisitExecutionItem> ExecutionItems => _executionItems.AsReadOnly();
    public IReadOnlyCollection<VisitPriceAdjustment> PriceAdjustments => _priceAdjustments.AsReadOnly();
    public decimal AppointmentTotalAmount => _executionItems.Sum(x => x.TotalAmount);
    public decimal AdjustmentTotalAmount => _priceAdjustments.Sum(x => x.Amount * x.Sign);
    public decimal FinalTotalAmount => AppointmentTotalAmount + AdjustmentTotalAmount;

    public static Visit CheckIn(
        Guid id,
        Guid appointmentId,
        IReadOnlyCollection<VisitExecutionItemDraft> executionItems,
        Guid? actorUserId,
        DateTime utcNow)
    {
        if (id == Guid.Empty)
        {
            throw new InvalidOperationException("Visit id is required.");
        }

        if (appointmentId == Guid.Empty)
        {
            throw new InvalidOperationException("Visit must reference an appointment.");
        }

        if (executionItems is null)
        {
            throw new InvalidOperationException("Visit execution items are required.");
        }

        if (executionItems.Count == 0)
        {
            throw new InvalidOperationException("Visit must include at least one execution item.");
        }

        var checkedInAtUtc = StampUtc(utcNow);
        var visit = new Visit
        {
            Id = id,
            AppointmentId = appointmentId,
            Status = VisitStatusCodes.Open,
            CheckedInAtUtc = checkedInAtUtc,
            CreatedByUserId = actorUserId,
            UpdatedByUserId = actorUserId,
            CreatedAtUtc = checkedInAtUtc,
            UpdatedAtUtc = checkedInAtUtc
        };

        foreach (var item in executionItems)
        {
            visit.AddExecutionItem(item, checkedInAtUtc);
        }

        return visit;
    }

    public VisitExecutionItem GetExecutionItem(Guid visitExecutionItemId)
    {
        if (visitExecutionItemId == Guid.Empty)
        {
            throw new InvalidOperationException("Visit execution item id is required.");
        }

        return _executionItems.SingleOrDefault(x => x.Id == visitExecutionItemId)
               ?? throw new InvalidOperationException("Visit execution item does not exist.");
    }

    public void EnsureCanBeCompleted()
    {
        if (Status is not VisitStatusCodes.Open and not VisitStatusCodes.InProgress)
        {
            throw new InvalidOperationException("Visit is not eligible for completion.");
        }
    }

    public void EnsureCanBeClosed()
    {
        if (Status != VisitStatusCodes.AwaitingFinalization)
        {
            throw new InvalidOperationException("Visit is not eligible for closure.");
        }
    }

    public VisitPerformedProcedure RecordPerformedProcedure(
        Guid visitExecutionItemId,
        VisitPerformedProcedureDraft procedure,
        Guid? actorUserId,
        DateTime utcNow)
    {
        EnsureEditable();
        var executionItem = GetExecutionItem(visitExecutionItemId);
        var performedProcedure = executionItem.RecordPerformedProcedure(procedure, actorUserId, StampUtc(utcNow));
        MarkInProgressIfOpen(actorUserId, utcNow);
        return performedProcedure;
    }

    public VisitSkippedComponent RecordSkippedComponent(
        Guid visitExecutionItemId,
        VisitSkippedComponentDraft component,
        Guid? actorUserId,
        DateTime utcNow)
    {
        EnsureEditable();
        var executionItem = GetExecutionItem(visitExecutionItemId);
        var skippedComponent = executionItem.RecordSkippedComponent(component, actorUserId, StampUtc(utcNow));
        MarkInProgressIfOpen(actorUserId, utcNow);
        return skippedComponent;
    }

    public VisitPriceAdjustment ApplyPriceAdjustment(
        VisitPriceAdjustmentDraft adjustment,
        Guid? actorUserId,
        DateTime utcNow)
    {
        if (adjustment is null)
        {
            throw new InvalidOperationException("Visit price adjustment is required.");
        }

        EnsureEditable(allowAwaitingFinalization: true);

        if (adjustment.Amount <= 0)
        {
            throw new InvalidOperationException("Adjustment amount must be greater than zero.");
        }

        var roundedAmount = decimal.Round(adjustment.Amount, 2, MidpointRounding.AwayFromZero);
        if (FinalTotalAmount + (roundedAmount * adjustment.Sign) < 0)
        {
            throw new InvalidOperationException("Visit final total cannot be negative.");
        }

        var priceAdjustment = VisitPriceAdjustment.Create(
            Guid.NewGuid(),
            Id,
            adjustment.Sign,
            roundedAmount,
            adjustment.ReasonCode,
            adjustment.Note,
            actorUserId,
            StampUtc(utcNow));

        _priceAdjustments.Add(priceAdjustment);
        Touch(actorUserId, utcNow);
        return priceAdjustment;
    }

    public void Complete(Guid? actorUserId, DateTime utcNow)
    {
        EnsureCanBeCompleted();
        CompletedAtUtc = StampUtc(utcNow);
        Status = VisitStatusCodes.AwaitingFinalization;
        Touch(actorUserId, CompletedAtUtc.Value);
    }

    public void Close(Guid? actorUserId, DateTime utcNow)
    {
        EnsureCanBeClosed();
        ClosedAtUtc = StampUtc(utcNow);
        Status = VisitStatusCodes.Closed;
        Touch(actorUserId, ClosedAtUtc.Value);
    }

    private void AddExecutionItem(VisitExecutionItemDraft item, DateTime utcNow)
    {
        if (item is null)
        {
            throw new InvalidOperationException("Visit execution item is required.");
        }

        if (_executionItems.Any(x => x.AppointmentItemId == item.AppointmentItemId))
        {
            throw new InvalidOperationException("Visit execution item already exists for this appointment item.");
        }

        _executionItems.Add(VisitExecutionItem.Create(Guid.NewGuid(), Id, item, utcNow));
    }

    private void MarkInProgressIfOpen(Guid? actorUserId, DateTime utcNow)
    {
        if (Status != VisitStatusCodes.Open)
        {
            return;
        }

        StartedAtUtc = StampUtc(utcNow);
        Status = VisitStatusCodes.InProgress;
        Touch(actorUserId, StartedAtUtc.Value);
    }

    private void EnsureEditable(bool allowAwaitingFinalization = false)
    {
        var isEditable = Status is VisitStatusCodes.Open or VisitStatusCodes.InProgress
                         || (allowAwaitingFinalization && Status == VisitStatusCodes.AwaitingFinalization);
        if (!isEditable)
        {
            throw new InvalidOperationException("Visit is not editable in its current status.");
        }
    }

    private void Touch(Guid? actorUserId, DateTime utcNow)
    {
        UpdatedAtUtc = StampUtc(utcNow);
        UpdatedByUserId = actorUserId;
    }

    private static DateTime StampUtc(DateTime value)
    {
        return DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }
}
