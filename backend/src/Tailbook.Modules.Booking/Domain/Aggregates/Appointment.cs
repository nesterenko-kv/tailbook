using ErrorOr;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.Modules.Booking.Domain.Events;

namespace Tailbook.Modules.Booking.Domain.Aggregates;

public sealed class Appointment : AggregateRoot
{
    private readonly List<AppointmentItem> _items = [];

    private Appointment()
    {
    }

    public Guid? BookingRequestId { get; private set; }
    public Guid PetId { get; private set; }
    public Guid GroomerId { get; private set; }
    public DateTimeOffset StartAt { get; private set; }
    public DateTimeOffset EndAt { get; private set; }
    public string Status { get; private set; } = string.Empty;
    public int VersionNo { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }
    public string? CancellationReasonCode { get; private set; }
    public string? CancellationNotes { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public IReadOnlyCollection<AppointmentItem> Items => _items.AsReadOnly();
    public BookingPeriod Period => BookingPeriod.Create(StartAt, EndAt).Value;

    public static ErrorOr<Appointment> Create(
        Guid id,
        Guid? bookingRequestId,
        Guid petId,
        Guid groomerId,
        BookingPeriod period,
        IReadOnlyCollection<AppointmentItemDraft> items,
        Guid? actorUserId,
        DateTimeOffset utcNow)
    {
        List<Error> errors = [];

        if (period is null)
        {
            errors.Add(AppointmentErrors.PeriodRequired);
        }

        if (items is null)
        {
            errors.Add(AppointmentErrors.ItemsRequired);
        }

        if (id == Guid.Empty)
        {
            errors.Add(AppointmentErrors.IdRequired);
        }

        if (petId == Guid.Empty)
        {
            errors.Add(AppointmentErrors.PetRequired);
        }

        if (groomerId == Guid.Empty)
        {
            errors.Add(AppointmentErrors.GroomerRequired);
        }

        if (items is not null && items.Count == 0)
        {
            errors.Add(AppointmentErrors.AtLeastOneItemRequired);
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        var validatedPeriod = period!;
        var validatedItems = items!;
        var appointment = new Appointment
        {
            Id = id,
            BookingRequestId = bookingRequestId,
            PetId = petId,
            GroomerId = groomerId,
            StartAt = validatedPeriod.StartAt,
            EndAt = validatedPeriod.EndAt,
            Status = AppointmentStatusCodes.Confirmed,
            VersionNo = 1,
            CreatedByUserId = actorUserId,
            UpdatedByUserId = actorUserId,
            CreatedAt = utcNow.ToUniversalTime(),
            UpdatedAt = utcNow.ToUniversalTime()
        };

        foreach (var item in validatedItems)
        {
            var itemResult = appointment.AddItem(item, utcNow);
            if (itemResult.IsError)
            {
                errors.AddRange(itemResult.Errors);
            }
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        appointment.RaiseDomainEvent(new AppointmentCreatedDomainEvent(
            Guid.NewGuid(),
            utcNow.ToUniversalTime(),
            appointment.Id,
            appointment.BookingRequestId,
            appointment.PetId,
            appointment.GroomerId,
            appointment.StartAt,
            appointment.EndAt,
            appointment.Status,
            appointment.VersionNo));

        return appointment;
    }

    public bool HasVersion(int expectedVersionNo)
    {
        return VersionNo == expectedVersionNo;
    }

    public ErrorOr<Success> EnsureCanBeRescheduled()
    {
        return EnsureMutable();
    }

    public ErrorOr<Success> EnsureCanBeCancelled()
    {
        return EnsureMutable();
    }

    public ErrorOr<Success> Reschedule(Guid groomerId, BookingPeriod period, Guid? actorUserId, DateTimeOffset now)
    {
        List<Error> errors = [];
        var mutable = EnsureMutable();
        if (mutable.IsError)
        {
            errors.AddRange(mutable.Errors);
        }

        if (period is null)
        {
            errors.Add(AppointmentErrors.PeriodRequired);
        }

        if (groomerId == Guid.Empty)
        {
            errors.Add(AppointmentErrors.GroomerRequired);
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        var validatedPeriod = period!;
        GroomerId = groomerId;
        StartAt = validatedPeriod.StartAt;
        EndAt = validatedPeriod.EndAt;
        Status = AppointmentStatusCodes.Rescheduled;
        Touch(actorUserId, now);
        RaiseDomainEvent(new AppointmentRescheduledDomainEvent(
            Guid.NewGuid(),
            now.ToUniversalTime(),
            Id,
            GroomerId,
            StartAt,
            EndAt,
            Status,
            VersionNo));
        return Result.Success;
    }

    public ErrorOr<Success> Cancel(string reasonCode, string? notes, Guid? actorUserId, DateTimeOffset now)
    {
        var mutable = EnsureMutable();
        if (mutable.IsError)
        {
            return mutable.Errors;
        }

        var normalizedReasonCode = NormalizeReasonCode(reasonCode);
        if (normalizedReasonCode.IsError)
        {
            return normalizedReasonCode.Errors;
        }

        var normalizedNotes = NormalizeOptional(notes);

        Status = AppointmentStatusCodes.Cancelled;
        CancellationReasonCode = normalizedReasonCode.Value;
        CancellationNotes = normalizedNotes;
        CancelledAt = now.ToUniversalTime();
        Touch(actorUserId, now);
        RaiseDomainEvent(new AppointmentCancelledDomainEvent(
            Guid.NewGuid(),
            now.ToUniversalTime(),
            Id,
            Status,
            CancellationReasonCode,
            CancellationNotes,
            VersionNo));
        return Result.Success;
    }

    public ErrorOr<Success> MarkCheckedIn(Guid? actorUserId, DateTimeOffset now)
    {
        if (Status is not AppointmentStatusCodes.Confirmed and not AppointmentStatusCodes.Rescheduled)
        {
            return AppointmentErrors.CheckInNotAllowed;
        }

        Status = AppointmentStatusCodes.CheckedIn;
        Touch(actorUserId, now);
        return Result.Success;
    }

    public ErrorOr<Success> MarkInProgress(Guid? actorUserId, DateTimeOffset now)
    {
        if (Status == AppointmentStatusCodes.InProgress)
        {
            return Result.Success;
        }

        if (Status != AppointmentStatusCodes.CheckedIn)
        {
            return AppointmentErrors.InProgressNotAllowed;
        }

        Status = AppointmentStatusCodes.InProgress;
        Touch(actorUserId, now);
        return Result.Success;
    }

    public ErrorOr<Success> MarkCompleted(Guid? actorUserId, DateTimeOffset now)
    {
        if (Status is not AppointmentStatusCodes.CheckedIn and not AppointmentStatusCodes.InProgress)
        {
            return AppointmentErrors.CompletionNotAllowed;
        }

        Status = AppointmentStatusCodes.Completed;
        Touch(actorUserId, now);
        return Result.Success;
    }

    public ErrorOr<Success> MarkClosed(Guid? actorUserId, DateTimeOffset now)
    {
        if (Status != AppointmentStatusCodes.Completed)
        {
            return AppointmentErrors.ClosureNotAllowed;
        }

        Status = AppointmentStatusCodes.Closed;
        Touch(actorUserId, now);
        return Result.Success;
    }

    private ErrorOr<Success> AddItem(AppointmentItemDraft item, DateTimeOffset utcNow)
    {
        if (item is null)
        {
            return AppointmentErrors.ItemRequired;
        }

        var appointmentItem = AppointmentItem.Create(
            Guid.NewGuid(),
            Id,
            item.ItemType,
            item.OfferId,
            item.OfferVersionId,
            item.OfferCodeSnapshot,
            item.OfferDisplayNameSnapshot,
            item.Quantity,
            item.PriceSnapshotId,
            item.DurationSnapshotId,
            utcNow);
        if (appointmentItem.IsError)
        {
            return appointmentItem.Errors;
        }

        _items.Add(appointmentItem.Value);
        return Result.Success;
    }

    private ErrorOr<Success> EnsureMutable()
    {
        if (Status is AppointmentStatusCodes.Cancelled or AppointmentStatusCodes.Closed)
        {
            return AppointmentErrors.NotMutable;
        }

        return Result.Success;
    }

    private void Touch(Guid? actorUserId, DateTimeOffset utcNow)
    {
        VersionNo += 1;
        UpdatedAt = utcNow.ToUniversalTime();
        UpdatedByUserId = actorUserId;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static ErrorOr<string> NormalizeReasonCode(string reasonCode)
    {
        if (string.IsNullOrWhiteSpace(reasonCode))
        {
            return AppointmentErrors.CancellationReasonRequired;
        }

        return reasonCode.Trim().ToUpperInvariant();
    }
}
