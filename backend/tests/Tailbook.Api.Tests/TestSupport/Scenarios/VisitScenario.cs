using System.Net.Http.Json;
using Tailbook.Api.Tests.TestSupport.Http;
using Tailbook.Api.Tests.TestSupport.Models;

namespace Tailbook.Api.Tests.TestSupport.Scenarios;

internal sealed class VisitScenario
{
    private readonly HttpClient _client;

    internal VisitScenario(
        HttpClient client,
        Guid clientId,
        Guid petId,
        VisitReadyOffer offer,
        Guid groomerId,
        AppointmentEnvelope appointment)
    {
        _client = client;
        ClientId = clientId;
        PetId = petId;
        Offer = offer;
        GroomerId = groomerId;
        Appointment = appointment;
    }

    public Guid ClientId { get; }
    public Guid PetId { get; }
    public VisitReadyOffer Offer { get; }
    public Guid GroomerId { get; }
    public AppointmentEnvelope Appointment { get; }

    public static VisitScenarioBuilder For(HttpClient client)
        => new(client);

    public async Task<HttpResponseMessage> CheckInResponseAsync()
        => await _client.PostAsJsonAsync($"/api/admin/appointments/{Appointment.Id:D}/check-in", new { appointmentId = Appointment.Id });

    public async Task<VisitEnvelope> CheckInAsync()
    {
        var response = await CheckInResponseAsync();
        response.ShouldBeCreated();
        return await response.ReadRequiredJsonAsync<VisitEnvelope>();
    }

    public async Task<VisitEnvelope> AddPerformedProcedureAsync(Guid visitId, Guid visitExecutionItemId, Guid procedureId, string note)
    {
        var response = await _client.PostAsJsonAsync($"/api/admin/visits/{visitId:D}/performed-procedures", new
        {
            visitId,
            visitExecutionItemId,
            procedureId,
            note
        });
        response.ShouldBeOk();
        return await response.ReadRequiredJsonAsync<VisitEnvelope>();
    }

    public async Task<VisitEnvelope> SkipExpectedComponentAsync(
        Guid visitId,
        Guid visitExecutionItemId,
        Guid offerVersionComponentId,
        string omissionReasonCode,
        string note)
    {
        var response = await _client.PostAsJsonAsync($"/api/admin/visits/{visitId:D}/skipped-components", new
        {
            visitId,
            visitExecutionItemId,
            offerVersionComponentId,
            omissionReasonCode,
            note
        });
        response.ShouldBeOk();
        return await response.ReadRequiredJsonAsync<VisitEnvelope>();
    }

    public async Task<VisitEnvelope> ApplyAdjustmentAsync(Guid visitId, int sign, decimal amount, string reasonCode, string note)
    {
        var response = await _client.PostAsJsonAsync($"/api/admin/visits/{visitId:D}/adjustments", new
        {
            visitId,
            sign,
            amount,
            reasonCode,
            note
        });
        response.ShouldBeOk();
        return await response.ReadRequiredJsonAsync<VisitEnvelope>();
    }

    public async Task<VisitEnvelope> CompleteAsync(Guid visitId)
    {
        var response = await _client.PostAsJsonAsync($"/api/admin/visits/{visitId:D}/complete", new { visitId });
        response.ShouldBeOk();
        return await response.ReadRequiredJsonAsync<VisitEnvelope>();
    }

    public async Task<VisitEnvelope> CloseAsync(Guid visitId)
    {
        var response = await _client.PostAsJsonAsync($"/api/admin/visits/{visitId:D}/close", new { visitId });
        response.ShouldBeOk();
        return await response.ReadRequiredJsonAsync<VisitEnvelope>();
    }

    public async Task<string> GetAppointmentStatusAsync()
    {
        var response = await _client.GetAsync($"/api/admin/appointments/{Appointment.Id:D}");
        response.EnsureSuccessStatusCode();
        return (await response.ReadRequiredJsonAsync<AppointmentEnvelope>()).Status;
    }

    public async Task<HttpResponseMessage> GetVisitDetailResponseAsync(Guid visitId)
        => await _client.GetAsync($"/api/admin/visits/{visitId:D}");
}
