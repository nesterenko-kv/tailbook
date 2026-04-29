using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Booking;
using Tailbook.Modules.Booking.Contracts;
using Tailbook.Modules.Booking.Domain;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class BookingAppointmentAggregateTests
{
    [Fact]
    public void Create_builds_confirmed_appointment_with_child_items()
    {
        var appointmentId = Guid.NewGuid();
        var actorUserId = Guid.NewGuid();
        var period = new BookingPeriod(Utc("2026-04-22T07:00:00Z"), Utc("2026-04-22T08:30:00Z"));

        var appointment = Appointment.Create(
            appointmentId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            period,
            [CreateItemDraft()],
            actorUserId,
            Utc("2026-04-21T12:00:00Z"));

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
    public void Create_rejects_invalid_appointment_shape()
    {
        Assert.Throws<InvalidOperationException>(() => new BookingPeriod(Utc("2026-04-22T08:30:00Z"), Utc("2026-04-22T07:00:00Z")));

        var exception = Assert.Throws<InvalidOperationException>(() => Appointment.Create(
            Guid.NewGuid(),
            null,
            Guid.NewGuid(),
            Guid.NewGuid(),
            new BookingPeriod(Utc("2026-04-22T07:00:00Z"), Utc("2026-04-22T08:30:00Z")),
            [],
            null,
            Utc("2026-04-21T12:00:00Z")));

        Assert.Equal("At least one appointment item is required.", exception.Message);
    }

    [Fact]
    public void Lifecycle_methods_allow_expected_state_transitions()
    {
        var appointment = CreateAppointment();

        appointment.MarkCheckedIn(null, Utc("2026-04-22T07:00:00Z"));
        appointment.MarkInProgress(null, Utc("2026-04-22T07:05:00Z"));
        appointment.MarkInProgress(null, Utc("2026-04-22T07:06:00Z"));
        appointment.MarkCompleted(null, Utc("2026-04-22T08:20:00Z"));
        appointment.MarkClosed(null, Utc("2026-04-22T08:30:00Z"));

        Assert.Equal(AppointmentStatusCodes.Closed, appointment.Status);
        Assert.Equal(5, appointment.VersionNo);
    }

    [Fact]
    public void Lifecycle_methods_reject_forbidden_state_transitions()
    {
        var appointment = CreateAppointment();

        Assert.Throws<InvalidOperationException>(() => appointment.MarkCompleted(null, Utc("2026-04-22T08:20:00Z")));

        appointment.Cancel(" client_request ", "  Customer changed plans.  ", null, Utc("2026-04-22T06:00:00Z"));

        Assert.Equal(AppointmentStatusCodes.Cancelled, appointment.Status);
        Assert.Equal("CLIENT_REQUEST", appointment.CancellationReasonCode);
        Assert.Equal("Customer changed plans.", appointment.CancellationNotes);
        Assert.Throws<InvalidOperationException>(() => appointment.Reschedule(
            Guid.NewGuid(),
            new BookingPeriod(Utc("2026-04-22T09:00:00Z"), Utc("2026-04-22T10:30:00Z")),
            null,
            Utc("2026-04-22T06:05:00Z")));
    }

    [Fact]
    public async Task Appointment_aggregate_round_trips_through_ef_core()
    {
        new BookingModule().ConfigurePersistence();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"booking-appointment-{Guid.NewGuid():N}")
            .Options;

        var appointment = CreateAppointment();
        await using (var dbContext = new AppDbContext(options))
        {
            dbContext.Set<Appointment>().Add(appointment);
            await dbContext.SaveChangesAsync();
        }

        await using (var dbContext = new AppDbContext(options))
        {
            var loaded = await dbContext.Set<Appointment>()
                .Include(x => x.Items)
                .SingleAsync(x => x.Id == appointment.Id);

            Assert.Equal(AppointmentStatusCodes.Confirmed, loaded.Status);
            Assert.Equal(appointment.Period, loaded.Period);
            var item = Assert.Single(loaded.Items);
            Assert.Equal(appointment.Id, item.AppointmentId);
            Assert.Equal("Package", item.ItemType);
        }
    }

    private static Appointment CreateAppointment()
    {
        return Appointment.Create(
            Guid.NewGuid(),
            null,
            Guid.NewGuid(),
            Guid.NewGuid(),
            new BookingPeriod(Utc("2026-04-22T07:00:00Z"), Utc("2026-04-22T08:30:00Z")),
            [CreateItemDraft()],
            null,
            Utc("2026-04-21T12:00:00Z"));
    }

    private static AppointmentItemDraft CreateItemDraft()
    {
        return new AppointmentItemDraft(
            " Package ",
            Guid.NewGuid(),
            Guid.NewGuid(),
            " BASIC ",
            " Basic Groom ",
            1,
            Guid.NewGuid(),
            Guid.NewGuid());
    }

    private static DateTime Utc(string value)
    {
        return DateTime.Parse(value).ToUniversalTime();
    }
}
