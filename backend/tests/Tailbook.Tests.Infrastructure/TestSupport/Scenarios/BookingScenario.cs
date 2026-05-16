using System.Globalization;
using System.Net.Http.Json;
using Tailbook.Api.Tests.TestSupport.Auth;
using Tailbook.Api.Tests.TestSupport.Http;
using Tailbook.Api.Tests.TestSupport.Models;
using Tailbook.Api.Tests;
using Xunit;

namespace Tailbook.Api.Tests.TestSupport.Scenarios;

public sealed class BookingScenario
{
    private readonly HttpClient _admin;
    private readonly RealDbWebApplicationFactory _factory;
    private Guid _petId;
    private Guid _offerId;
    private Guid _groomerId;

    private BookingScenario(HttpClient admin, RealDbWebApplicationFactory factory)
    {
        _admin = admin;
        _factory = factory;
    }

    public static Builder For(HttpClient admin) => new(admin);

    public sealed class Builder(HttpClient admin)
    {
        private readonly HttpClient _admin = admin;
        private readonly RealDbWebApplicationFactory _factory = null!;
        private string _clientDisplayName = "Default Client";
        private string _offerDisplayName = "Default Offer";
        private string _groomerDisplayName = "Default Groomer";

        public Builder WithSchedulablePet(string clientDisplayName)
        {
            _clientDisplayName = clientDisplayName;
            return this;
        }

        public Builder WithSchedulableOffer(string? displayName = null)
        {
            if (displayName is not null)
                _offerDisplayName = displayName;
            return this;
        }

        public Builder WithAvailableGroomer(string groomerDisplayName)
        {
            _groomerDisplayName = groomerDisplayName;
            return this;
        }

        public async Task<BookingScenario> CreateAsync()
        {
            var scenario = new BookingScenario(_admin, _factory);
            scenario._petId = Guid.NewGuid();
            scenario._offerId = Guid.NewGuid();
            scenario._groomerId = Guid.NewGuid();
            await Task.CompletedTask;
            return scenario;
        }
    }

    public async Task<AppointmentSummaryItem> CreateBookingRequestAsync(
        DateTimeOffset preferredStartAt, DateTimeOffset preferredEndAt)
    {
        var response = await _admin.PostAsJsonAsync("/api/admin/booking-requests", new
        {
            petId = _petId,
            offerId = _offerId,
            groomerId = _groomerId,
            preferredStartAt,
            preferredEndAt
        });
        response.ShouldBeCreated();
        return await response.ReadRequiredJsonAsync<AppointmentSummaryItem>();
    }

    public async Task<AppointmentSummaryItem> ConvertBookingRequestAsync(
        Guid bookingRequestId, DateTimeOffset startAt)
    {
        var response = await _admin.PostAsJsonAsync($"/api/admin/booking-requests/{bookingRequestId}/convert", new
        {
            startAt
        });
        response.ShouldBeOk();
        return await response.ReadRequiredJsonAsync<AppointmentSummaryItem>();
    }

    public async Task<PagedBookingRequestEnvelope> ListBookingRequestsAsync()
    {
        var response = await _admin.GetAsync("/api/admin/booking-requests");
        response.ShouldBeOk();
        return await response.ReadRequiredJsonAsync<PagedBookingRequestEnvelope>();
    }

    public async Task<AppointmentSummaryItem> CreateAppointmentAsync(DateTimeOffset startAt)
    {
        var response = await _admin.PostAsJsonAsync("/api/admin/appointments", new
        {
            petId = _petId,
            offerId = _offerId,
            groomerId = _groomerId,
            startAt
        });
        response.ShouldBeCreated();
        return await response.ReadRequiredJsonAsync<AppointmentSummaryItem>();
    }

    public async Task<AppointmentSummaryItem> RescheduleAppointmentAsync(
        Guid appointmentId, DateTimeOffset startAt, int expectedVersionNo)
    {
        var response = await _admin.PutAsJsonAsync($"/api/admin/appointments/{appointmentId}/reschedule", new
        {
            groomerId = _groomerId,
            startAt,
            expectedVersionNo
        });
        response.ShouldBeOk();
        return await response.ReadRequiredJsonAsync<AppointmentSummaryItem>();
    }

    public async Task<HttpResponseMessage> CancelAppointmentResponseAsync(
        Guid appointmentId, int expectedVersionNo)
    {
        return await _admin.PostAsJsonAsync($"/api/admin/appointments/{appointmentId}/cancel", new
        {
            reasonCode = "CLIENT_REQUEST",
            expectedVersionNo
        });
    }

    public async Task<AppointmentSummaryItem> CancelAppointmentAsync(
        Guid appointmentId, int expectedVersionNo, string? notes = null)
    {
        var response = await CancelAppointmentResponseAsync(appointmentId, expectedVersionNo);
        response.ShouldBeOk();
        return await response.ReadRequiredJsonAsync<AppointmentSummaryItem>();
    }

    public async Task<GroomerAvailabilityResult> CheckGroomerAvailabilityAsync(
        DateTimeOffset startAt, int reservedMinutes)
    {
        var url = $"/api/admin/groomers/{_groomerId}/availability?startAt={Uri.EscapeDataString(startAt.ToString("O", CultureInfo.InvariantCulture))}&reservedMinutes={reservedMinutes}";
        var response = await _admin.GetAsync(url);
        response.ShouldBeOk();
        return await response.ReadRequiredJsonAsync<GroomerAvailabilityResult>();
    }
}

