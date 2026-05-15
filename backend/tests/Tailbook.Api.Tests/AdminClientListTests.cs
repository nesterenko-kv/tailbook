using System.Net.Http.Json;
using Tailbook.Api.Tests.TestSupport.Auth;
using Tailbook.Api.Tests.TestSupport.Http;
using Tailbook.Api.Tests.TestSupport.Scenarios;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class AdminClientListTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Admin_can_search_clients_by_display_name_contact_and_method()
    {
        using var client = await factory.CreateAdminClientAsync();

        var customers = CustomerScenario.For(client);
        var matchedClientId = await customers.CreateClientAsync("Searchable Client");
        await customers.CreateClientAsync("Unmatched Client");
        var contactId = await customers.AddContactAsync(matchedClientId, "Nina", "Needle");
        await customers.AddContactMethodAsync(contactId, "Phone", "+1 (555) 0101", "Desk line 555-0101");

        var displayNameSearch = await client.GetFromJsonAsync<PagedClientsEnvelope>("/api/admin/clients?search=searchable");
        Assert.NotNull(displayNameSearch);
        Assert.Contains(displayNameSearch.Items, x => x.Id == matchedClientId);

        var contactSearch = await client.GetFromJsonAsync<PagedClientsEnvelope>("/api/admin/clients?search=needle");
        Assert.NotNull(contactSearch);
        Assert.Contains(contactSearch.Items, x => x.Id == matchedClientId);

        var methodSearch = await client.GetFromJsonAsync<PagedClientsEnvelope>("/api/admin/clients?search=555-0101");
        Assert.NotNull(methodSearch);
        Assert.Contains(methodSearch.Items, x => x.Id == matchedClientId);
    }

    [Fact]
    public async Task Admin_client_list_requires_client_read_permission()
    {
        using var anonymousClient = factory.CreateAnonymousClient();
        var anonymousResponse = await anonymousClient.GetAsync("/api/admin/clients");
        anonymousResponse.ShouldBeUnauthorized();

        using var groomerClient = await factory.CreateClientForRoleAsync(
            "groomer-clients@test.local",
            "Client List Groomer",
            TestUsers.GroomerPassword,
            "groomer");

        var forbiddenResponse = await groomerClient.GetAsync("/api/admin/clients");
        forbiddenResponse.ShouldBeForbidden();
    }

    private sealed class PagedClientsEnvelope
    {
        public ClientListItemEnvelope[] Items { get; set; } = [];
    }

    private sealed class ClientListItemEnvelope
    {
        public Guid Id { get; set; }
    }
}
