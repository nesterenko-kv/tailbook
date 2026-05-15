using System.Net.Http.Json;
using Tailbook.Api.Tests.TestSupport.Http;
using Tailbook.Api.Tests.TestSupport.Models;

namespace Tailbook.Api.Tests.TestSupport.Scenarios;

internal sealed class AdminBookingApi(HttpClient client)
{
    public static AdminBookingApi For(HttpClient client)
        => new(client);

    public async Task<AppointmentEnvelope> CreateAppointmentAsync(
        Guid petId,
        Guid groomerId,
        Guid offerId,
        DateTimeOffset? startAt = null)
    {
        var response = await client.PostAsJsonAsync("/api/admin/appointments", new
        {
            petId,
            groomerId,
            startAt = startAt ?? ApiClientExtensions.UtcDateTime("2026-04-24T07:00:00Z"),
            items = new[] { new { offerId, itemType = "Package" } }
        });
        response.ShouldBeCreated();
        return await response.ReadRequiredJsonAsync<AppointmentEnvelope>();
    }
}
