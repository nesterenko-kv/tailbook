using Tailbook.Modules.Booking.Contracts;

namespace Tailbook.Modules.Booking.Domain.Aggregates;

public sealed class Appointment
{
    private readonly List<AppointmentItem> _items = [];

    private Appointment()
    {
    }

    public Guid Id { get; private set; }
    public Guid? BookingRequestId { get; private set; }
    public Guid PetId { get; private set; }
    public Guid GroomerId { get; private set; }
    public DateTime StartAtUtc { get; private set; }
    public DateTime EndAtUtc { get; private set; }
    public string Status { get; private set; } = string.Empty;
    public int VersionNo { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }
    public string? CancellationReasonCode { get; private set; }
    public string? CancellationNotes { get; private set; }
    public DateTime? CancelledAtUtc { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }
    public IReadOnlyCollection<AppointmentItem> Items => _items.AsReadOnly();
    public BookingPeriod Period => new(StartAtUtc, EndAtUtc);

    public static Appointment Create(
        Guid id,
        Guid? bookingRequestId,
        Guid petId,
        Guid groomerId,
        BookingPeriod period,
        IReadOnlyCollection<AppointmentItemDraft> items,
        Guid? actorUserId,
        DateTime utcNow)
    {
        if (period is null)
        {
            throw new InvalidOperationException("Appointment period is required.");
        }

        if (items is null)
        {
            throw new InvalidOperationException("Appointment items are required.");
        }

        if (id == Guid.Empty)
        {
            throw new InvalidOperationException("Appointment id is required.");
        }

        if (petId == Guid.Empty)
        {
            throw new InvalidOperationException("Appointment must reference a pet.");
        }

        if (groomerId == Guid.Empty)
        {
            throw new InvalidOperationException("Appointment must reference a groomer.");
        }

        if (items.Count == 0)
        {
            throw new InvalidOperationException("At least one appointment item is required.");
        }

        var appointment = new Appointment
        {
            Id = id,
            BookingRequestId = bookingRequestId,
            PetId = petId,
            GroomerId = groomerId,
            StartAtUtc = period.StartAtUtc,
            EndAtUtc = period.EndAtUtc,
            Status = AppointmentStatusCodes.Confirmed,
            VersionNo = 1,
            CreatedByUserId = actorUserId,
            UpdatedByUserId = actorUserId,
            CreatedAtUtc = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc),
            UpdatedAtUtc = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc)
        };

        foreach (var item in items)
        {
            appointment.AddItem(item, utcNow);
        }

        return appointment;
    }

    public bool HasVersion(int expectedVersionNo)
    {
        return VersionNo == expectedVersionNo;
    }

    public void EnsureCanBeRescheduled()
    {
        EnsureMutable();
    }

    public void EnsureCanBeCancelled()
    {
        EnsureMutable();
    }

    public void Reschedule(Guid groomerId, BookingPeriod period, Guid? actorUserId, DateTime utcNow)
    {
        EnsureMutable();

        if (period is null)
        {
            throw new InvalidOperationException("Appointment period is required.");
        }

        if (groomerId == Guid.Empty)
        {
            throw new InvalidOperationException("Appointment must reference a groomer.");
        }

        GroomerId = groomerId;
        StartAtUtc = period.StartAtUtc;
        EndAtUtc = period.EndAtUtc;
        Status = AppointmentStatusCodes.Rescheduled;
        Touch(actorUserId, utcNow);
    }

    public void Cancel(string reasonCode, string? notes, Guid? actorUserId, DateTime utcNow)
    {
        EnsureMutable();
        var normalizedReasonCode = NormalizeReasonCode(reasonCode);
        var normalizedNotes = NormalizeOptional(notes);

        Status = AppointmentStatusCodes.Cancelled;
        CancellationReasonCode = normalizedReasonCode;
        CancellationNotes = normalizedNotes;
        CancelledAtUtc = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
        Touch(actorUserId, utcNow);
    }

    public void MarkCheckedIn(Guid? actorUserId, DateTime utcNow)
    {
        if (Status is not AppointmentStatusCodes.Confirmed and not AppointmentStatusCodes.Rescheduled)
        {
            throw new InvalidOperationException("Appointment is not eligible for check-in.");
        }

        Status = AppointmentStatusCodes.CheckedIn;
        Touch(actorUserId, utcNow);
    }

    public void MarkInProgress(Guid? actorUserId, DateTime utcNow)
    {
        if (Status == AppointmentStatusCodes.InProgress)
        {
            return;
        }

        if (Status != AppointmentStatusCodes.CheckedIn)
        {
            throw new InvalidOperationException("Appointment is not eligible to enter in-progress state.");
        }

        Status = AppointmentStatusCodes.InProgress;
        Touch(actorUserId, utcNow);
    }

    public void MarkCompleted(Guid? actorUserId, DateTime utcNow)
    {
        if (Status is not AppointmentStatusCodes.CheckedIn and not AppointmentStatusCodes.InProgress)
        {
            throw new InvalidOperationException("Appointment is not eligible for completion.");
        }

        Status = AppointmentStatusCodes.Completed;
        Touch(actorUserId, utcNow);
    }

    public void MarkClosed(Guid? actorUserId, DateTime utcNow)
    {
        if (Status != AppointmentStatusCodes.Completed)
        {
            throw new InvalidOperationException("Appointment is not eligible for closure.");
        }

        Status = AppointmentStatusCodes.Closed;
        Touch(actorUserId, utcNow);
    }

    private void AddItem(AppointmentItemDraft item, DateTime utcNow)
    {
        if (item is null)
        {
            throw new InvalidOperationException("Appointment item is required.");
        }

        _items.Add(AppointmentItem.Create(
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
            utcNow));
    }

    private void EnsureMutable()
    {
        if (Status is AppointmentStatusCodes.Cancelled or AppointmentStatusCodes.Closed)
        {
            throw new InvalidOperationException("Appointment is not mutable in its current status.");
        }
    }

    private void Touch(Guid? actorUserId, DateTime utcNow)
    {
        VersionNo += 1;
        UpdatedAtUtc = DateTime.SpecifyKind(utcNow, DateTimeKind.Utc);
        UpdatedByUserId = actorUserId;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string NormalizeReasonCode(string reasonCode)
    {
        if (string.IsNullOrWhiteSpace(reasonCode))
        {
            throw new InvalidOperationException("Cancellation reason code is required.");
        }

        return reasonCode.Trim().ToUpperInvariant();
    }
}
