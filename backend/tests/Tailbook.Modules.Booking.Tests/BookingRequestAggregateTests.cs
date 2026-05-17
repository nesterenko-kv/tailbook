using Tailbook.Modules.Booking.Contracts;
using Tailbook.Modules.Booking.Domain.Events;
using Xunit;

namespace Tailbook.Modules.Booking.Tests;

public sealed class BookingRequestAggregateTests
{
    [Fact]
    public void Create_raises_booking_requested_domain_event()
    {
        var bookingRequestId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var clientId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var petId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var requestedAt = Utc("2026-04-22T08:00:00Z");

        var request = BookingRequest.Create(
            bookingRequestId,
            clientId,
            petId,
            requestedByContactId: null,
            preferredGroomerId: null,
            channel: BookingChannelCodes.ClientPortal,
            status: BookingRequestStatusCodes.Submitted,
            selectionMode: BookingRequestSelectionModeCodes.ExactSlot,
            guestIntakeJson: null,
            preferredTimeJson: null,
            notes: "Please be gentle",
            utcNow: requestedAt);

        var domainEvent = Assert.IsType<BookingRequestedDomainEvent>(Assert.Single(request.GetDomainEvents()));
        Assert.Equal(bookingRequestId, domainEvent.BookingRequestId);
        Assert.Equal(clientId, domainEvent.ClientId);
        Assert.Equal(petId, domainEvent.PetId);
        Assert.Equal(BookingChannelCodes.ClientPortal, domainEvent.Channel);
        Assert.Equal(BookingRequestStatusCodes.Submitted, domainEvent.Status);
        Assert.Equal(BookingRequestSelectionModeCodes.ExactSlot, domainEvent.SelectionMode);
    }

    [Fact]
    public void Mark_converted_raises_booking_request_converted_domain_event()
    {
        var request = BookingRequest.Create(
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            requestedByContactId: null,
            preferredGroomerId: null,
            channel: BookingChannelCodes.ClientPortal,
            status: BookingRequestStatusCodes.Submitted,
            selectionMode: BookingRequestSelectionModeCodes.ExactSlot,
            guestIntakeJson: null,
            preferredTimeJson: null,
            notes: null,
            utcNow: Utc("2026-04-22T08:00:00Z"));
        request.ClearDomainEvents();
        var appointmentId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

        request.MarkConverted(appointmentId, Utc("2026-04-22T08:05:00Z"));

        var domainEvent = Assert.IsType<BookingRequestConvertedDomainEvent>(Assert.Single(request.GetDomainEvents()));
        Assert.Equal(request.Id, domainEvent.BookingRequestId);
        Assert.Equal(appointmentId, domainEvent.AppointmentId);
    }

    private static DateTimeOffset Utc(string value)
    {
        return DateTimeOffset.Parse(value).ToUniversalTime();
    }
}
