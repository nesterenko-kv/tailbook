namespace Tailbook.Modules.Pets.Api.Admin.ListPets;

public sealed class ListPetsRequest
{
    public string? Search { get; set; }
    public Guid? ClientId { get; set; }
    public string? AnimalTypeCode { get; set; }
    public Guid? BreedId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}