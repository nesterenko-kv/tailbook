namespace Tailbook.Modules.Customer.Api.Admin.ListPetContactLinks;

public sealed class ListPetContactLinksResponse
{
    public IReadOnlyCollection<PetContactLinkResponse> Items { get; set; } = [];
}
