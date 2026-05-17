using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.BuildingBlocks.Infrastructure.Persistence.Integration;
using Tailbook.Api.Tests.Factories;
using Xunit;

namespace Tailbook.Modules.Booking.Tests;

public sealed class BookingFlowTests(RealDbWebApplicationFactory factory) : IClassFixture<RealDbWebApplicationFactory>
{
    [Fact]
    public async Task Admin_can_create_booking_request_and_convert_it_to_appointment()
    {
        using var admin = await factory.CreateAdminClientAsync();

        var scenario = await BookingScenario
            .For(admin)
            .WithSchedulablePet("Booking Client")
            .WithSchedulableOffer(displayName: "Booking Package")
            .WithAvailableGroomer("Booking Conversion Groomer")
            .CreateAsync();

        var bookingRequest = await scenario.CreateBookingRequestAsync(
            preferredStartAt: ApiClientExtensions.UtcDateTime("2026-04-22T10:00:00Z"),
            preferredEndAt: ApiClientExtensions.UtcDateTime("2026-04-22T13:00:00Z"));

        Assert.Equal("Submitted", bookingRequest.Status);

        var appointment = await scenario.ConvertBookingRequestAsync(
            bookingRequest.Id,
            startAt: ApiClientExtensions.UtcDateTime("2026-04-22T07:00:00Z"));

        appointment.ShouldBeConvertedFrom(bookingRequest.Id);

        var requestList = await scenario.ListBookingRequestsAsync();
        Assert.Contains(requestList.Items, x => x.Id == bookingRequest.Id && x.Status == "Converted");

        var searchedRequests = await admin.GetFromJsonAsync<PagedBookingRequestEnvelope>("/api/admin/booking-requests?search=milo");
        Assert.NotNull(searchedRequests);
        Assert.Contains(searchedRequests.Items, x => x.Id == bookingRequest.Id);

        await admin.AssertAuditEntryEventuallyExistsAsync(
            moduleCode: "booking",
            entityType: "booking_request",
            entityId: bookingRequest.Id,
            actionCode: "CONVERT_TO_APPOINTMENT",
            failureMessage: "Booking request conversion audit entry was not persisted.");
    }

    [Fact]
    public async Task Admin_can_create_reschedule_and_cancel_appointment_with_optimistic_concurrency()
    {
        using var admin = await factory.CreateAdminClientAsync();

        var scenario = await BookingScenario
            .For(admin)
            .WithSchedulablePet("Appointment Client")
            .WithSchedulableOffer()
            .WithAvailableGroomer("Appointment Lifecycle Groomer")
            .CreateAsync();

        var appointment = await scenario.CreateAppointmentAsync(
            startAt: ApiClientExtensions.UtcDateTime("2026-04-23T07:00:00Z"));

        var searchedAppointments = await admin.GetFromJsonAsync<PagedAppointmentEnvelope>("/api/admin/appointments?search=booking%20package");
        Assert.NotNull(searchedAppointments);
        Assert.Contains(searchedAppointments.Items, x => x.Id == appointment.Id);

        var rescheduled = await scenario.RescheduleAppointmentAsync(
            appointment.Id,
            startAt: ApiClientExtensions.UtcDateTime("2026-04-23T10:00:00Z"),
            expectedVersionNo: 1);

        rescheduled.ShouldHaveStatus("Rescheduled", versionNo: 2);

        var staleCancelResponse = await scenario.CancelAppointmentResponseAsync(
            appointment.Id,
            expectedVersionNo: 1);

        staleCancelResponse.ShouldBeConflict();

        var cancelled = await scenario.CancelAppointmentAsync(
            appointment.Id,
            expectedVersionNo: 2,
            notes: "Customer rescheduled elsewhere.");

        cancelled.ShouldHaveStatus("Cancelled", versionNo: 3);

        await admin.AssertAuditEntryEventuallyExistsAsync(
            moduleCode: "booking",
            entityType: "appointment",
            entityId: appointment.Id,
            actionCode: "CANCEL",
            failureMessage: "Appointment cancel audit entry was not persisted.");
    }

    [Fact]
    public async Task Existing_appointment_blocks_staff_availability_check()
    {
        using var admin = await factory.CreateAdminClientAsync();

        var scenario = await BookingScenario
            .For(admin)
            .WithSchedulablePet("Overlap Client")
            .WithSchedulableOffer()
            .WithAvailableGroomer("Availability Conflict Groomer")
            .CreateAsync();

        await scenario.CreateAppointmentAsync(
            startAt: ApiClientExtensions.UtcDateTime("2026-04-24T07:00:00Z"));

        var availability = await scenario.CheckGroomerAvailabilityAsync(
            startAt: ApiClientExtensions.UtcDateTime("2026-04-24T07:10:00Z"),
            reservedMinutes: 90);

        availability.ShouldBeUnavailableBecause("existing appointment");
    }

    [Fact]
    public async Task Cancel_appointment_stages_booking_event_in_outbox()
    {
        using var admin = await factory.CreateAdminClientAsync();

        var scenario = await BookingScenario
            .For(admin)
            .WithSchedulablePet("Outbox Booking Client")
            .WithSchedulableOffer()
            .WithAvailableGroomer("Outbox Booking Groomer")
            .CreateAsync();

        var appointment = await scenario.CreateAppointmentAsync(
            startAt: ApiClientExtensions.UtcDateTime("2026-04-23T07:00:00Z"));

        var cancelled = await scenario.CancelAppointmentAsync(
            appointment.Id,
            expectedVersionNo: 1,
            notes: "No longer needed.");

        cancelled.ShouldHaveStatus("Cancelled", versionNo: 2);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var outboxMessage = await dbContext.Set<OutboxMessage>()
            .OrderByDescending(x => x.OccurredAt)
            .FirstAsync(x => x.ModuleCode == "booking" && x.EventType == "AppointmentCancelled");

        using var payload = JsonDocument.Parse(outboxMessage.PayloadJson);
        Assert.Equal("booking", outboxMessage.ModuleCode);
        Assert.Equal("AppointmentCancelled", outboxMessage.EventType);
        Assert.Equal(appointment.Id, payload.RootElement.GetProperty("appointmentId").GetGuid());
        Assert.Equal("Cancelled", payload.RootElement.GetProperty("status").GetString());
        Assert.Equal(2, payload.RootElement.GetProperty("versionNo").GetInt32());
    }
}
