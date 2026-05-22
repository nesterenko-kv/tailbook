using ErrorOr;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.Modules.VisitOperations.Domain.Events;

namespace Tailbook.Modules.VisitOperations.Domain.Aggregates;

public sealed class Visit : AggregateRoot
{
    private readonly List<VisitExecutionItem> _executionItems = [];
    private readonly List<VisitPriceAdjustment> _priceAdjustments = [];

    private Visit()
    {
    }

    public Guid AppointmentId { get; private set; }
    public string Status { get; private set; } = string.Empty;
    public DateTimeOffset CheckedInAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public IReadOnlyCollection<VisitExecutionItem> ExecutionItems => _executionItems.AsReadOnly();
    public IReadOnlyCollection<VisitPriceAdjustment> PriceAdjustments => _priceAdjustments.AsReadOnly();
    public decimal AppointmentTotalAmount => _executionItems.Sum(x => x.TotalAmount);
    public decimal AdjustmentTotalAmount => _priceAdjustments.Sum(x => x.Amount * x.Sign);
    public decimal FinalTotalAmount => AppointmentTotalAmount + AdjustmentTotalAmount;

    public static ErrorOr<Visit> CheckIn(
        Guid id,
        Guid appointmentId,
        IReadOnlyCollection<VisitExecutionItemDraft> executionItems,
        Guid? actorUserId,
        DateTimeOffset utcNow)
    {
        List<Error> errors = [];

        if (id == Guid.Empty)
        {
            errors.Add(VisitErrors.IdRequired);
        }

        if (appointmentId == Guid.Empty)
        {
            errors.Add(VisitErrors.AppointmentRequired);
        }

        if (executionItems is null)
        {
            errors.Add(VisitErrors.ExecutionItemsRequired);
        }

        if (executionItems is not null && executionItems.Count == 0)
        {
            errors.Add(VisitErrors.AtLeastOneExecutionItemRequired);
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        var validatedExecutionItems = executionItems!;
        var checkedInAt = StampUtc(utcNow);
        var visit = new Visit
        {
            Id = id,
            AppointmentId = appointmentId,
            Status = VisitStatusCodes.Open,
            CheckedInAt = checkedInAt,
            CreatedByUserId = actorUserId,
            UpdatedByUserId = actorUserId,
            CreatedAt = checkedInAt,
            UpdatedAt = checkedInAt
        };

        foreach (var item in validatedExecutionItems)
        {
            var itemResult = visit.AddExecutionItem(item, checkedInAt);
            if (itemResult.IsError)
            {
                errors.AddRange(itemResult.Errors);
            }
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        visit.RaiseDomainEvent(new VisitCheckedInDomainEvent(
            Guid.NewGuid(),
            checkedInAt,
            visit.Id,
            visit.AppointmentId,
            visit.Status,
            visit.CheckedInAt));

        return visit;
    }

    public ErrorOr<VisitExecutionItem> GetExecutionItem(Guid visitExecutionItemId)
    {
        if (visitExecutionItemId == Guid.Empty)
        {
            return VisitErrors.ExecutionItemIdRequired;
        }

        var executionItem = _executionItems.SingleOrDefault(x => x.Id == visitExecutionItemId);
        return executionItem is null
            ? VisitErrors.ExecutionItemNotFound
            : executionItem;
    }

    public ErrorOr<Success> EnsureCanBeCompleted()
    {
        if (Status is not VisitStatusCodes.Open and not VisitStatusCodes.InProgress)
        {
            return VisitErrors.CompletionFailed;
        }

        return Result.Success;
    }

    public ErrorOr<Success> EnsureCanBeClosed()
    {
        if (Status != VisitStatusCodes.AwaitingFinalization)
        {
            return VisitErrors.CloseFailed;
        }

        return Result.Success;
    }

    public ErrorOr<VisitPerformedProcedure> RecordPerformedProcedure(
        Guid visitExecutionItemId,
        VisitPerformedProcedureDraft procedure,
        Guid? actorUserId,
        DateTimeOffset utcNow)
    {
        var editable = EnsureEditable();
        if (editable.IsError)
        {
            return editable.Errors;
        }

        var executionItem = GetExecutionItem(visitExecutionItemId);
        if (executionItem.IsError)
        {
            return executionItem.Errors;
        }

        var performedProcedure = executionItem.Value.RecordPerformedProcedure(procedure, actorUserId, StampUtc(utcNow));
        if (performedProcedure.IsError)
        {
            return performedProcedure.Errors;
        }

        MarkInProgressIfOpen(actorUserId, utcNow);
        return performedProcedure.Value;
    }

    public ErrorOr<VisitSkippedComponent> RecordSkippedComponent(
        Guid visitExecutionItemId,
        VisitSkippedComponentDraft component,
        Guid? actorUserId,
        DateTimeOffset utcNow)
    {
        var editable = EnsureEditable();
        if (editable.IsError)
        {
            return editable.Errors;
        }

        var executionItem = GetExecutionItem(visitExecutionItemId);
        if (executionItem.IsError)
        {
            return executionItem.Errors;
        }

        var skippedComponent = executionItem.Value.RecordSkippedComponent(component, actorUserId, StampUtc(utcNow));
        if (skippedComponent.IsError)
        {
            return skippedComponent.Errors;
        }

        MarkInProgressIfOpen(actorUserId, utcNow);
        return skippedComponent.Value;
    }

    public ErrorOr<VisitPriceAdjustment> ApplyPriceAdjustment(
        VisitPriceAdjustmentDraft adjustment,
        Guid? actorUserId,
        DateTimeOffset utcNow)
    {
        if (adjustment is null)
        {
            return VisitErrors.PriceAdjustmentRequired;
        }

        var editable = EnsureEditable(allowAwaitingFinalization: true);
        if (editable.IsError)
        {
            return editable.Errors;
        }

        if (adjustment.Amount <= 0)
        {
            return VisitErrors.InvalidAdjustmentAmount;
        }

        var roundedAmount = decimal.Round(adjustment.Amount, 2, MidpointRounding.AwayFromZero);
        if (FinalTotalAmount + (roundedAmount * adjustment.Sign) < 0)
        {
            return VisitErrors.NegativeFinalTotal;
        }

        var priceAdjustment = VisitPriceAdjustment.Create(
            Guid.NewGuid(),
            Id,
            adjustment.TargetItemId,
            adjustment.Sign,
            roundedAmount,
            adjustment.ReasonCode,
            adjustment.Note,
            actorUserId,
            StampUtc(utcNow));
        if (priceAdjustment.IsError)
        {
            return priceAdjustment.Errors;
        }

        _priceAdjustments.Add(priceAdjustment.Value);
        Touch(actorUserId, utcNow);
        RaiseDomainEvent(new FinalPriceAdjustedDomainEvent(
            Guid.NewGuid(),
            StampUtc(utcNow),
            Id,
            Status,
            priceAdjustment.Value.Sign,
            priceAdjustment.Value.Amount,
            priceAdjustment.Value.ReasonCode));
        return priceAdjustment.Value;
    }

    public ErrorOr<Success> Complete(Guid? actorUserId, DateTimeOffset now)
    {
        var canBeCompleted = EnsureCanBeCompleted();
        if (canBeCompleted.IsError)
        {
            return canBeCompleted.Errors;
        }

        CompletedAt = StampUtc(now);
        Status = VisitStatusCodes.AwaitingFinalization;
        Touch(actorUserId, CompletedAt.Value);
        RaiseDomainEvent(new VisitCompletedDomainEvent(
            Guid.NewGuid(),
            CompletedAt.Value,
            Id,
            AppointmentId,
            Status,
            CompletedAt.Value));
        return Result.Success;
    }

    public ErrorOr<Success> Close(Guid? actorUserId, DateTimeOffset now)
    {
        var canBeClosed = EnsureCanBeClosed();
        if (canBeClosed.IsError)
        {
            return canBeClosed.Errors;
        }

        ClosedAt = StampUtc(now);
        Status = VisitStatusCodes.Closed;
        Touch(actorUserId, ClosedAt.Value);
        RaiseDomainEvent(new VisitClosedDomainEvent(
            Guid.NewGuid(),
            ClosedAt.Value,
            Id,
            AppointmentId,
            Status,
            FinalTotalAmount,
            ClosedAt.Value));
        return Result.Success;
    }

    private ErrorOr<Success> AddExecutionItem(VisitExecutionItemDraft item, DateTimeOffset utcNow)
    {
        if (item is null)
        {
            return VisitErrors.ExecutionItemRequired;
        }

        if (_executionItems.Any(x => x.AppointmentItemId == item.AppointmentItemId))
        {
            return VisitErrors.ExecutionItemAlreadyExists;
        }

        var executionItem = VisitExecutionItem.Create(Guid.NewGuid(), Id, item, utcNow);
        if (executionItem.IsError)
        {
            return executionItem.Errors;
        }

        _executionItems.Add(executionItem.Value);
        return Result.Success;
    }

    private void MarkInProgressIfOpen(Guid? actorUserId, DateTimeOffset utcNow)
    {
        if (Status != VisitStatusCodes.Open)
        {
            return;
        }

        StartedAt = StampUtc(utcNow);
        Status = VisitStatusCodes.InProgress;
        Touch(actorUserId, StartedAt.Value);
    }

    private ErrorOr<Success> EnsureEditable(bool allowAwaitingFinalization = false)
    {
        var isEditable = Status is VisitStatusCodes.Open or VisitStatusCodes.InProgress
                         || (allowAwaitingFinalization && Status == VisitStatusCodes.AwaitingFinalization);
        if (!isEditable)
        {
            return VisitErrors.NotEditable;
        }

        return Result.Success;
    }

    private void Touch(Guid? actorUserId, DateTimeOffset utcNow)
    {
        UpdatedAt = StampUtc(utcNow);
        UpdatedByUserId = actorUserId;
    }

    private static DateTimeOffset StampUtc(DateTimeOffset value)
    {
        return value.ToUniversalTime();
    }
}
