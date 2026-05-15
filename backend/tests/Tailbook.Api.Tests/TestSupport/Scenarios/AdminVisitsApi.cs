using System.Net.Http.Json;
using Tailbook.Api.Tests.TestSupport.Http;
using Tailbook.Api.Tests.TestSupport.Models;

namespace Tailbook.Api.Tests.TestSupport.Scenarios;

internal sealed class AdminVisitsApi(HttpClient client)
{
    public static AdminVisitsApi For(HttpClient client)
        => new(client);

    public async Task<VisitEnvelope> CheckInAsync(Guid appointmentId)
    {
        var response = await client.PostAsJsonAsync($"/api/admin/appointments/{appointmentId:D}/check-in", new { appointmentId });
        response.ShouldBeCreated();
        return await response.ReadRequiredJsonAsync<VisitEnvelope>();
    }
}
