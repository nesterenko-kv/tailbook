using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Tailbook.Api.Tests;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Booking.Contracts;
using Xunit;

namespace Tailbook.Modules.Booking.Tests;

public sealed class BookingAppointmentAggregateTests
{
    [Fact]
    public void Create_builds_confirmed_appointment_with_child_items()
    {
        var appointmentId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var period = CreatePeriod("2026-04-22T07:00:00Z", "2026-04-22T08:30:00Z");

        var result = Appointment.Create(
            appointmentId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            period,
            [CreateItemDraft()],
            actorUserId,
            Utc("2026-04-21T12:00:00Z"));

        var appointment = AssertSuccess(result);
        Assert.Equal(appointmentId, appointment.Id);
        Assert.Equal(AppointmentStatusCodes.Confirmed, appointment.Status);
        Assert.Equal(1, appointment.VersionNo);
        Assert.Equal(period, appointment.Period);
        var item = Assert.Single(appointment.Items);
        Assert.Equal(appointment.Id, item.AppointmentId);
        Assert.Equal("Package", item.ItemType);
        Assert.Equal(1, item.Quantity);
    }

    [Fact]
    public void Items_collection_cannot_be_externally_mutated()
    {
        var appointment = CreateAppointment();

        var items = Assert.IsAssignableFrom<ICollection<AppointmentItem>>(appointment.Items);

        Assert.True(items.IsReadOnly);
        Assert.Throws<NotSupportedException>(items.Clear);
        Assert.Single(appointment.Items);
    }

    [Fact]
    public void Create_rejects_empty_ids()
    {
        var period = CreatePeriod("2026-04-22T07:00:00Z", "2026-04-22T08:30:00Z");

        AssertError(Appointment.Create(
            Guid.Empty,
            null,
            Guid.NewGuid(),
            Guid.NewGuid(),
            period,
            [CreateItemDraft()],
            null,
            Utc("2026-04-21T12:00:00Z")));

        AssertError(Appointment.Create(
            Guid.NewGuid(),
            null,
            Guid.Empty,
            Guid.NewGuid(),
            period,
            [CreateItemDraft()],
            null,
            Utc("2026-04-21T12:00:00Z")));

        AssertError(Appointment.Create(
            Guid.NewGuid(),
            null,
            Guid.NewGuid(),
            Guid.Empty,
            period,
            [CreateItemDraft()],
            null,
            Utc("2026-04-21T12:00:00Z")));
    }

    [Fact]
    public void Create_returns_all_top_level_validation_errors()
    {
        var result = Appointment.Create(
            Guid.Empty,
            null,
            Guid.Empty,
            Guid.Empty,
            null!,
            null!,
            null,
            Utc("2026-04-21T12:00:00Z"));

        AssertErrorCodes(
            result,
            "Booking.AppointmentPeriodRequired",
            "Booking.AppointmentItemsRequired",
            "Booking.AppointmentIdRequired",
            "Booking.AppointmentPetRequired",
            "Booking.AppointmentGroomerRequired");
    }

    [Fact]
    public void Create_rejects_empty_item_list()
    {
        var result = Appointment.Create(
            Guid.NewGuid(),
            null,
            Guid.NewGuid(),
            Guid.NewGuid(),
            CreatePeriod("2026-04-22T07:00:00Z", "2026-04-22T08:30:00Z"),
            [],
            null,
            Utc("2026-04-21T12:00:00Z"));

        var error = Assert.Single(AssertError(result));
        Assert.Equal("At least one appointment item is required.", error.Description);
    }

    [Fact]
    public void Create_rejects_invalid_item_drafts()
    {
        AssertInvalidDraft(CreateItemDraft(offerId: Guid.Empty));
        AssertInvalidDraft(CreateItemDraft(offerVersionId: Guid.Empty));
        AssertInvalidDraft(CreateItemDraft(itemType: " "));
        AssertInvalidDraft(CreateItemDraft(offerCodeSnapshot: " "));
        AssertInvalidDraft(CreateItemDraft(offerDisplayNameSnapshot: " "));
        AssertInvalidDraft(CreateItemDraft(quantity: 0));
        AssertInvalidDraft(CreateItemDraft(priceSnapshotId: Guid.Empty));
        AssertInvalidDraft(CreateItemDraft(durationSnapshotId: Guid.Empty));
    }

    [Fact]
    public void Create_returns_all_invalid_item_draft_errors()
    {
        var result = Appointment.Create(
            Guid.NewGuid(),
            null,
            Guid.NewGuid(),
            Guid.NewGuid(),
            CreatePeriod("2026-04-22T07:00:00Z", "2026-04-22T08:30:00Z"),
            [CreateItemDraft(
                itemType: " ",
                offerId: Guid.Empty,
                offerVersionId: Guid.Empty,
                offerCodeSnapshot: " ",
                offerDisplayNameSnapshot: " ",
                quantity: 0,
                priceSnapshotId: Guid.Empty,
                durationSnapshotId: Guid.Empty)],
            null,
            Utc("2026-04-21T12:00:00Z"));

        AssertErrorCodes(
            result,
            "Booking.AppointmentItemTypeRequired",
            "Booking.AppointmentItemOfferRequired",
            "Booking.AppointmentItemOfferVersionRequired",
            "Booking.AppointmentItemOfferCodeRequired",
            "Booking.AppointmentItemOfferDisplayNameRequired",
            "Booking.AppointmentItemQuantityInvalid",
            "Booking.AppointmentItemPriceSnapshotRequired",
            "Booking.AppointmentItemDurationSnapshotRequired");
    }

    [Fact]
    public void BookingPeriod_rejects_invalid_values()
    {
        AssertError(BookingPeriod.Create(default, Utc("2026-04-22T08:30:00Z")));
        AssertError(BookingPeriod.Create(
            DateTimeOffset.Parse("2026-04-22T10:00:00+03:00"),
            Utc("2026-04-22T08:30:00Z")));
        AssertError(BookingPeriod.Create(Utc("2026-04-22T08:30:00Z"), Utc("2026-04-22T07:00:00Z")));
    }

    [Fact]
    public void BookingPeriod_returns_all_endpoint_validation_errors()
    {
        AssertErrorCodes(
            BookingPeriod.Create(default, default),
            "Booking.startAtRequired",
            "Booking.endAtRequired");
    }

    [Fact]
    public void Reschedule_updates_period_status_and_version()
    {
        var appointment = CreateAppointment();
        var groomerId = Guid.NewGuid();
        var period = CreatePeriod("2026-04-22T09:00:00Z", "2026-04-22T10:30:00Z");

        AssertSuccess(appointment.Reschedule(groomerId, period, null, Utc("2026-04-22T06:00:00Z")));

        Assert.Equal(groomerId, appointment.GroomerId);
        Assert.Equal(period, appointment.Period);
        Assert.Equal(AppointmentStatusCodes.Rescheduled, appointment.Status);
        Assert.Equal(2, appointment.VersionNo);
    }

    [Fact]
    public void Reschedule_rejects_missing_period_without_mutating_state()
    {
        var appointment = CreateAppointment();
        var originalGroomerId = appointment.GroomerId;
        var originalPeriod = appointment.Period;

        AssertError(appointment.Reschedule(
            Guid.NewGuid(),
            null!,
            null,
            Utc("2026-04-22T06:00:00Z")));

        Assert.Equal(originalGroomerId, appointment.GroomerId);
        Assert.Equal(originalPeriod, appointment.Period);
        Assert.Equal(AppointmentStatusCodes.Confirmed, appointment.Status);
        Assert.Equal(1, appointment.VersionNo);
    }

    [Fact]
    public void Lifecycle_methods_allow_expected_state_transitions()
    {
        var appointment = CreateAppointment();

        AssertSuccess(appointment.MarkCheckedIn(null, Utc("2026-04-22T07:00:00Z")));
        Assert.Equal(AppointmentStatusCodes.CheckedIn, appointment.Status);

        AssertSuccess(appointment.MarkInProgress(null, Utc("2026-04-22T07:05:00Z")));
        Assert.Equal(AppointmentStatusCodes.InProgress, appointment.Status);

        AssertSuccess(appointment.MarkInProgress(null, Utc("2026-04-22T07:06:00Z")));
        AssertSuccess(appointment.MarkCompleted(null, Utc("2026-04-22T08:20:00Z")));
        Assert.Equal(AppointmentStatusCodes.Completed, appointment.Status);

        AssertSuccess(appointment.MarkClosed(null, Utc("2026-04-22T08:30:00Z")));

        Assert.Equal(AppointmentStatusCodes.Closed, appointment.Status);
        Assert.Equal(5, appointment.VersionNo);
    }

    [Fact]
    public void Lifecycle_methods_reject_forbidden_transition_path()
    {
        var appointment = CreateAppointment();

        AssertError(appointment.MarkCompleted(null, Utc("2026-04-22T08:20:00Z")));
        Assert.Equal(AppointmentStatusCodes.Confirmed, appointment.Status);
        Assert.Equal(1, appointment.VersionNo);
    }

    [Fact]
    public void Cancel_normalizes_reason_and_blocks_duplicate_cancellation()
    {
        var appointment = CreateAppointment();
        AssertSuccess(appointment.Cancel(" client_request ", "  Customer changed plans.  ", null, Utc("2026-04-22T06:00:00Z")));

        Assert.Equal(AppointmentStatusCodes.Cancelled, appointment.Status);
        Assert.Equal("CLIENT_REQUEST", appointment.CancellationReasonCode);
        Assert.Equal("Customer changed plans.", appointment.CancellationNotes);
        Assert.Equal(2, appointment.VersionNo);
        AssertError(appointment.Cancel("CLIENT_REQUEST", null, null, Utc("2026-04-22T06:01:00Z")));
    }

    [Fact]
    public void Cancel_rejects_invalid_reason_without_mutating_state()
    {
        var appointment = CreateAppointment();

        AssertError(appointment.Cancel(" ", null, null, Utc("2026-04-22T06:00:00Z")));

        Assert.Equal(AppointmentStatusCodes.Confirmed, appointment.Status);
        Assert.Null(appointment.CancellationReasonCode);
        Assert.Null(appointment.CancelledAt);
        Assert.Equal(1, appointment.VersionNo);
    }

    [Fact]
    public void Terminal_appointments_cannot_be_rescheduled()
    {
        var cancelled = CreateAppointment();
        AssertSuccess(cancelled.Cancel("CLIENT_REQUEST", null, null, Utc("2026-04-22T06:00:00Z")));
        AssertError(cancelled.Reschedule(
            Guid.NewGuid(),
            CreatePeriod("2026-04-22T09:00:00Z", "2026-04-22T10:30:00Z"),
            null,
            Utc("2026-04-22T06:05:00Z")));

        var closed = CreateAppointment();
        AssertSuccess(closed.MarkCheckedIn(null, Utc("2026-04-22T07:00:00Z")));
        AssertSuccess(closed.MarkCompleted(null, Utc("2026-04-22T08:20:00Z")));
        AssertSuccess(closed.MarkClosed(null, Utc("2026-04-22T08:30:00Z")));
        AssertError(closed.Reschedule(
            Guid.NewGuid(),
            CreatePeriod("2026-04-22T09:00:00Z", "2026-04-22T10:30:00Z"),
            null,
            Utc("2026-04-22T08:35:00Z")));
    }

    [Fact]
    public async Task Appointment_aggregate_round_trips_through_ef_core()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"booking-appointment-{Guid.NewGuid():N}")
            .Options;

        var appointment = CreateAppointment();
        await using (var dbContext = TestModelConfiguration.CreateDbContext(options))
        {
            dbContext.Set<Appointment>().Add(appointment);
            await dbContext.SaveChangesAsync();
        }

        await using (var dbContext = TestModelConfiguration.CreateDbContext(options))
        {
            var loaded = await dbContext.Set<Appointment>()
                .AsNoTracking()
                .Include(x => x.Items)
                .SingleAsync(x => x.Id == appointment.Id);

            Assert.Equal(AppointmentStatusCodes.Confirmed, loaded.Status);
            Assert.Equal(appointment.Period, loaded.Period);
            var item = Assert.Single(loaded.Items);
            Assert.Equal(appointment.Id, item.AppointmentId);
            Assert.Equal("Package", item.ItemType);
        }
    }

    private static void AssertInvalidDraft(AppointmentItemDraft draft)
    {
        AssertError(Appointment.Create(
            Guid.NewGuid(),
            null,
            Guid.NewGuid(),
            Guid.NewGuid(),
            CreatePeriod("2026-04-22T07:00:00Z", "2026-04-22T08:30:00Z"),
            [draft],
            null,
            Utc("2026-04-21T12:00:00Z")));
    }

    private static Appointment CreateAppointment()
    {
        return AssertSuccess(Appointment.Create(
            Guid.NewGuid(),
            null,
            Guid.NewGuid(),
            Guid.NewGuid(),
            CreatePeriod("2026-04-22T07:00:00Z", "2026-04-22T08:30:00Z"),
            [CreateItemDraft()],
            null,
            Utc("2026-04-21T12:00:00Z")));
    }

    private static AppointmentItemDraft CreateItemDraft(
        string itemType = " Package ",
        Guid? offerId = null,
        Guid? offerVersionId = null,
        string offerCodeSnapshot = " BASIC ",
        string offerDisplayNameSnapshot = " Basic Groom ",
        int quantity = 1,
        Guid? priceSnapshotId = null,
        Guid? durationSnapshotId = null)
    {
        return new AppointmentItemDraft(
            itemType,
            offerId ?? Guid.NewGuid(),
            offerVersionId ?? Guid.NewGuid(),
            offerCodeSnapshot,
            offerDisplayNameSnapshot,
            quantity,
            priceSnapshotId ?? Guid.NewGuid(),
            durationSnapshotId ?? Guid.NewGuid());
    }

    private static BookingPeriod CreatePeriod(string startAt, string endAt)
    {
        return AssertSuccess(BookingPeriod.Create(Utc(startAt), Utc(endAt)));
    }

    private static T AssertSuccess<T>(ErrorOr<T> result)
    {
        Assert.False(result.IsError, string.Join("; ", result.Errors.Select(error => error.Description)));
        return result.Value;
    }

    private static IReadOnlyList<Error> AssertError<T>(ErrorOr<T> result)
    {
        Assert.True(result.IsError);
        return result.Errors;
    }

    private static void AssertErrorCodes<T>(ErrorOr<T> result, params string[] expectedCodes)
    {
        var codes = AssertError(result).Select(error => error.Code).ToArray();
        Assert.Equal(expectedCodes, codes);
    }

    private static DateTimeOffset Utc(string value)
    {
        return DateTimeOffset.Parse(value).ToUniversalTime();
    }
}
