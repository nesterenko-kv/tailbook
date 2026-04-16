using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class CustomerPetsFlowTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CustomerPetsFlowTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Admin_can_create_client_contact_pet_link_and_read_details()
    {
        var token = await _factory.LoginAsAsync("admin@test.local", "Admin12345!");
        using var client = _factory.CreateClient();
        CustomWebApplicationFactory.SetBearer(client, token);

        var createClientResponse = await client.PostAsJsonAsync("/api/admin/clients", new
        {
            displayName = "Olena H.",
            notes = "Instagram first"
        });

        Assert.Equal(HttpStatusCode.Created, createClientResponse.StatusCode);
        var createdClient = await createClientResponse.Content.ReadFromJsonAsync<CreateClientResponse>();
        Assert.NotNull(createdClient);

        var addContactResponse = await client.PostAsJsonAsync($"/api/admin/clients/{createdClient!.Id:D}/contacts", new
        {
            firstName = "Olena",
            lastName = "Hrytsenko",
            trustLevel = "Trusted"
        });

        Assert.Equal(HttpStatusCode.Created, addContactResponse.StatusCode);
        var contact = await addContactResponse.Content.ReadFromJsonAsync<ContactResponse>();
        Assert.NotNull(contact);

        var addMethodResponse = await client.PostAsJsonAsync($"/api/admin/contacts/{contact!.Id:D}/methods", new
        {
            methodType = "Instagram",
            value = "@olena.tailbook",
            isPreferred = true
        });

        Assert.Equal(HttpStatusCode.Created, addMethodResponse.StatusCode);

        var catalog = await client.GetFromJsonAsync<PetCatalogResponse>("/api/admin/pets/catalog");
        Assert.NotNull(catalog);
        var samoyedBreedId = catalog!.Breeds.Single(x => x.Code == "SAMOYED").Id;

        var registerPetResponse = await client.PostAsJsonAsync("/api/admin/pets", new
        {
            clientId = createdClient.Id,
            name = "Milo",
            animalTypeCode = "DOG",
            breedId = samoyedBreedId,
            coatTypeCode = "DOUBLE_COAT",
            sizeCategoryCode = "LARGE",
            notes = "Sensitive paws"
        });

        Assert.Equal(HttpStatusCode.Created, registerPetResponse.StatusCode);
        var pet = await registerPetResponse.Content.ReadFromJsonAsync<PetResponse>();
        Assert.NotNull(pet);

        var linkResponse = await client.PostAsJsonAsync($"/api/admin/pets/{pet!.Id:D}/contacts/{contact.Id:D}", new
        {
            roleCodes = new[] { "Owner", "NotificationRecipient" },
            isPrimary = true,
            canPickUp = true,
            canPay = true,
            receivesNotifications = true
        });

        Assert.Equal(HttpStatusCode.OK, linkResponse.StatusCode);

        var clientDetail = await client.GetFromJsonAsync<ClientDetailResponse>($"/api/admin/clients/{createdClient.Id:D}");
        Assert.NotNull(clientDetail);
        Assert.Single(clientDetail!.Contacts);
        Assert.Single(clientDetail.Pets);
        Assert.Equal("Milo", clientDetail.Pets.Single().Name);
        Assert.Single(clientDetail.Contacts.Single().Methods);

        var petContacts = await client.GetFromJsonAsync<PetContactsResponse>($"/api/admin/pets/{pet.Id:D}/contacts");
        Assert.NotNull(petContacts);
        Assert.Single(petContacts!.Items);
        Assert.Contains("Owner", petContacts.Items.Single().RoleCodes);
    }

    [Fact]
    public async Task Reading_client_detail_creates_access_audit_entry()
    {
        var token = await _factory.LoginAsAsync("admin@test.local", "Admin12345!");
        using var client = _factory.CreateClient();
        CustomWebApplicationFactory.SetBearer(client, token);

        var createClientResponse = await client.PostAsJsonAsync("/api/admin/clients", new { displayName = "Audit Client" });
        var createdClient = await createClientResponse.Content.ReadFromJsonAsync<CreateClientResponse>();
        Assert.NotNull(createdClient);

        var addContactResponse = await client.PostAsJsonAsync($"/api/admin/clients/{createdClient!.Id:D}/contacts", new { firstName = "Audit" });
        var contact = await addContactResponse.Content.ReadFromJsonAsync<ContactResponse>();
        Assert.NotNull(contact);

        await client.PostAsJsonAsync($"/api/admin/contacts/{contact!.Id:D}/methods", new { methodType = "Phone", value = "+380501112233", isPreferred = true });

        var detailResponse = await client.GetAsync($"/api/admin/clients/{createdClient.Id:D}");
        Assert.Equal(HttpStatusCode.OK, detailResponse.StatusCode);

        var audit = await client.GetFromJsonAsync<AuditResponse>($"/api/admin/audit/access?resourceType=crm_client&resourceId={createdClient.Id:D}");
        Assert.NotNull(audit);
        Assert.Contains(audit!.Items, x => x.ActionCode == "READ_CONTACT_DATA");
    }

    private sealed class CreateClientResponse
    {
        public Guid Id { get; set; }
    }

    private sealed class ContactResponse
    {
        public Guid Id { get; set; }
    }

    private sealed class PetCatalogResponse
    {
        public BreedItem[] Breeds { get; set; } = [];
    }

    private sealed class BreedItem
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
    }

    private sealed class PetResponse
    {
        public Guid Id { get; set; }
    }

    private sealed class ClientDetailResponse
    {
        public ContactItem[] Contacts { get; set; } = [];
        public PetItem[] Pets { get; set; } = [];
    }

    private sealed class ContactItem
    {
        public MethodItem[] Methods { get; set; } = [];
    }

    private sealed class MethodItem { }

    private sealed class PetItem
    {
        public string Name { get; set; } = string.Empty;
    }

    private sealed class PetContactsResponse
    {
        public PetContactItem[] Items { get; set; } = [];
    }

    private sealed class PetContactItem
    {
        public string[] RoleCodes { get; set; } = [];
    }

    private sealed class AuditResponse
    {
        public AuditItem[] Items { get; set; } = [];
    }

    private sealed class AuditItem
    {
        public string ActionCode { get; set; } = string.Empty;
    }
}
