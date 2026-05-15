using System.Net.Http.Json;
using Tailbook.Api.Tests.TestSupport.Http;
using Tailbook.Api.Tests.TestSupport.Models;

namespace Tailbook.Api.Tests.TestSupport.Scenarios;

internal sealed class CustomerScenario(HttpClient client)
{
    public static CustomerScenario For(HttpClient client)
        => new(client);

    public async Task<Guid> CreateClientAsync(string displayName)
    {
        var response = await client.PostAsJsonAsync("/api/admin/clients", new { displayName });
        response.EnsureSuccessStatusCode();
        return (await response.ReadRequiredJsonAsync<ClientEnvelope>()).Id;
    }

    public async Task<Guid> AddContactAsync(Guid clientId, string firstName, string lastName)
    {
        var response = await client.PostAsJsonAsync($"/api/admin/clients/{clientId:D}/contacts", new
        {
            clientId,
            firstName,
            lastName
        });
        response.EnsureSuccessStatusCode();
        return (await response.ReadRequiredJsonAsync<ContactEnvelope>()).Id;
    }

    public async Task AddContactMethodAsync(Guid contactId, string methodType, string value, string displayValue)
    {
        var response = await client.PostAsJsonAsync($"/api/admin/contacts/{contactId:D}/methods", new
        {
            contactId,
            methodType,
            value,
            displayValue,
            isPreferred = true
        });
        response.EnsureSuccessStatusCode();
    }
}
