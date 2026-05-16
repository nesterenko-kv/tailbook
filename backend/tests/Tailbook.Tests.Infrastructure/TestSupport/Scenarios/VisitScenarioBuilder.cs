using System.Net.Http.Json;
using Tailbook.Api.Tests.TestSupport.Http;
using Tailbook.Api.Tests.TestSupport.Models;

namespace Tailbook.Api.Tests.TestSupport.Scenarios;

public sealed class VisitScenarioBuilder(HttpClient client)
{
    private string _clientDisplayName = "Visit Client";
    private string? _petNotes;
    private string _offerCodePrefix = "VISIT";
    private string _offerDisplayName = "Visit Package";
    private decimal _fixedAmount = 1500m;
    private int _serviceMinutes = 120;
    private int _bufferBeforeMinutes = 5;
    private int _bufferAfterMinutes = 10;
    private string _groomerDisplayName = "Visit Groomer";
    private IEnumerable<int> _groomerWeekdays = [5];
    private DateTimeOffset _appointmentStartAt = ApiClientExtensions.UtcDateTime("2026-04-24T07:00:00Z");

    public VisitScenarioBuilder WithSchedulablePet(string clientDisplayName, string? petNotes = null)
    {
        _clientDisplayName = clientDisplayName;
        _petNotes = petNotes;
        return this;
    }

    public VisitScenarioBuilder WithVisitReadyOffer(
        string codePrefix = "VISIT",
        string displayName = "Visit Package",
        decimal fixedAmount = 1500m,
        int serviceMinutes = 120,
        int bufferBeforeMinutes = 5,
        int bufferAfterMinutes = 10)
    {
        _offerCodePrefix = codePrefix;
        _offerDisplayName = displayName;
        _fixedAmount = fixedAmount;
        _serviceMinutes = serviceMinutes;
        _bufferBeforeMinutes = bufferBeforeMinutes;
        _bufferAfterMinutes = bufferAfterMinutes;
        return this;
    }

    public VisitScenarioBuilder WithAvailableGroomer(
        string displayName = "Visit Groomer",
        IEnumerable<int>? weekdays = null)
    {
        _groomerDisplayName = displayName;
        _groomerWeekdays = weekdays ?? [5];
        return this;
    }

    public VisitScenarioBuilder WithAppointmentAt(DateTimeOffset startAt)
    {
        _appointmentStartAt = startAt;
        return this;
    }

    public async Task<VisitScenario> CreateAsync()
    {
        var pet = await PetScenario.For(client).CreateSchedulablePetAsync(_clientDisplayName, _petNotes);
        var offer = await CatalogScenario.For(client).CreateVisitReadyOfferAsync(
            pet.Catalog.SamoyedBreedId,
            _offerCodePrefix,
            _offerDisplayName,
            _fixedAmount,
            _serviceMinutes,
            _bufferBeforeMinutes,
            _bufferAfterMinutes);
        var groomer = await StaffScenario.For(client).CreateSchedulableGroomerAsync(_groomerDisplayName, weekdays: _groomerWeekdays);

        var appointmentResponse = await client.PostAsJsonAsync("/api/admin/appointments", new
        {
            petId = pet.PetId,
            groomerId = groomer.Id,
            startAt = _appointmentStartAt,
            items = new[] { new { offerId = offer.OfferId, itemType = "Package" } }
        });
        appointmentResponse.ShouldBeCreated();
        var appointment = await appointmentResponse.ReadRequiredJsonAsync<AppointmentEnvelope>();

        return new VisitScenario(client, pet.ClientId, pet.PetId, offer, groomer.Id, appointment);
    }
}
