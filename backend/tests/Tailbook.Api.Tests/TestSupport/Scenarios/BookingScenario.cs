using System.Net.Http.Json;
using Tailbook.Api.Tests.TestSupport.Http;
using Tailbook.Api.Tests.TestSupport.Models;

namespace Tailbook.Api.Tests.TestSupport.Scenarios;

internal sealed class BookingScenario
{
    private readonly HttpClient _client;

    internal BookingScenario(HttpClient client, Guid clientId, Guid petId, Guid offerId, Guid groomerId, PetCatalogSelection catalog)
    {
        _client = client;
        ClientId = clientId;
        PetId = petId;
        OfferId = offerId;
        GroomerId = groomerId;
        Catalog = catalog;
    }

    public Guid ClientId { get; }
    public Guid PetId { get; }
    public Guid OfferId { get; }
    public Guid GroomerId { get; }
    public PetCatalogSelection Catalog { get; }

    public static BookingScenarioBuilder For(HttpClient client)
        => new(client);

    public async Task<BookingRequestEnvelope> CreateBookingRequestAsync(DateTimeOffset preferredStartAt, DateTimeOffset preferredEndAt, string label = "Afternoon")
    {
        var response = await _client.PostAsJsonAsync("/api/admin/booking-requests", new
        {
            clientId = ClientId,
            petId = PetId,
            channel = "Admin",
            notes = "Customer prefers afternoon.",
            preferredTimes = new[]
            {
                new { startAt = preferredStartAt, endAt = preferredEndAt, label }
            },
            items = new[]
            {
                new { offerId = OfferId, itemType = "Package" }
            }
        });
        response.ShouldBeCreated();
        return await response.ReadRequiredJsonAsync<BookingRequestEnvelope>();
    }

    public async Task<AppointmentEnvelope> ConvertBookingRequestAsync(Guid bookingRequestId, DateTimeOffset startAt)
    {
        var response = await _client.PostAsJsonAsync($"/api/admin/booking-requests/{bookingRequestId:D}/convert", new
        {
            bookingRequestId,
            groomerId = GroomerId,
            startAt
        });
        response.ShouldBeCreated();
        return await response.ReadRequiredJsonAsync<AppointmentEnvelope>();
    }

    public async Task<PagedBookingRequestEnvelope> ListBookingRequestsAsync()
    {
        var response = await _client.GetAsync("/api/admin/booking-requests");
        response.ShouldBeOk();
        return await response.ReadRequiredJsonAsync<PagedBookingRequestEnvelope>();
    }

    public async Task<AppointmentEnvelope> CreateAppointmentAsync(DateTimeOffset startAt)
    {
        var response = await _client.PostAsJsonAsync("/api/admin/appointments", new
        {
            petId = PetId,
            groomerId = GroomerId,
            startAt,
            items = new[] { new { offerId = OfferId, itemType = "Package" } }
        });
        response.ShouldBeCreated();
        return await response.ReadRequiredJsonAsync<AppointmentEnvelope>();
    }

    public async Task<HttpResponseMessage> RescheduleAppointmentResponseAsync(Guid appointmentId, DateTimeOffset startAt, int expectedVersionNo)
        => await _client.PostAsJsonAsync($"/api/admin/appointments/{appointmentId:D}/reschedule", new
        {
            appointmentId,
            groomerId = GroomerId,
            startAt,
            expectedVersionNo
        });

    public async Task<AppointmentEnvelope> RescheduleAppointmentAsync(Guid appointmentId, DateTimeOffset startAt, int expectedVersionNo)
    {
        var response = await RescheduleAppointmentResponseAsync(appointmentId, startAt, expectedVersionNo);
        response.ShouldBeOk();
        return await response.ReadRequiredJsonAsync<AppointmentEnvelope>();
    }

    public async Task<HttpResponseMessage> CancelAppointmentResponseAsync(
        Guid appointmentId,
        int expectedVersionNo,
        string reasonCode = "CLIENT_REQUEST",
        string? notes = null)
        => await _client.PostAsJsonAsync($"/api/admin/appointments/{appointmentId:D}/cancel", new
        {
            appointmentId,
            expectedVersionNo,
            reasonCode,
            notes
        });

    public async Task<AppointmentEnvelope> CancelAppointmentAsync(
        Guid appointmentId,
        int expectedVersionNo,
        string reasonCode = "CLIENT_REQUEST",
        string? notes = null)
    {
        var response = await CancelAppointmentResponseAsync(appointmentId, expectedVersionNo, reasonCode, notes);
        response.ShouldBeOk();
        return await response.ReadRequiredJsonAsync<AppointmentEnvelope>();
    }

    public async Task<AvailabilityEnvelope> CheckGroomerAvailabilityAsync(DateTimeOffset startAt, int reservedMinutes)
        => await StaffScenario.For(_client).CheckAvailabilityAsync(GroomerId, PetId, startAt, reservedMinutes, OfferId);
}

internal sealed class BookingScenarioBuilder(HttpClient client)
{
    private string _clientDisplayName = "Booking Client";
    private string? _petNotes;
    private string _offerCodePrefix = "BOOK";
    private string _offerDisplayName = "Booking Package";
    private decimal _fixedAmount = 1500m;
    private int _serviceMinutes = 90;
    private IEnumerable<int> _groomerWeekdays = [1, 2, 3, 4, 5];
    private string _groomerDisplayName = "Booking Groomer";

    public BookingScenarioBuilder WithSchedulablePet(string clientDisplayName, string? petNotes = null)
    {
        _clientDisplayName = clientDisplayName;
        _petNotes = petNotes;
        return this;
    }

    public BookingScenarioBuilder WithSchedulableOffer(
        string codePrefix = "BOOK",
        string displayName = "Booking Package",
        decimal fixedAmount = 1500m,
        int serviceMinutes = 90)
    {
        _offerCodePrefix = codePrefix;
        _offerDisplayName = displayName;
        _fixedAmount = fixedAmount;
        _serviceMinutes = serviceMinutes;
        return this;
    }

    public BookingScenarioBuilder WithAvailableGroomer(
        string displayName = "Booking Groomer",
        IEnumerable<int>? weekdays = null)
    {
        _groomerDisplayName = displayName;
        _groomerWeekdays = weekdays ?? [1, 2, 3, 4, 5];
        return this;
    }

    public async Task<BookingScenario> CreateAsync()
    {
        var pet = await PetScenario.For(client).CreateSchedulablePetAsync(_clientDisplayName, _petNotes);
        var offerId = await CatalogScenario.For(client).CreateSchedulableOfferAsync(
            pet.Catalog.SamoyedBreedId,
            _offerCodePrefix,
            _offerDisplayName,
            _fixedAmount,
            _serviceMinutes);
        var groomer = await StaffScenario.For(client).CreateSchedulableGroomerAsync(_groomerDisplayName, weekdays: _groomerWeekdays);

        return new BookingScenario(client, pet.ClientId, pet.PetId, offerId, groomer.Id, pet.Catalog);
    }
}
