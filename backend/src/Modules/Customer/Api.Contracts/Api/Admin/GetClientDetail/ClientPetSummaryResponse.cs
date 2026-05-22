namespace Tailbook.Modules.Customer.Api.Admin.GetClientDetail;

public sealed class ClientPetSummaryResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AnimalTypeCode { get; set; } = string.Empty;
    public string AnimalTypeName { get; set; } = string.Empty;
    public string BreedName { get; set; } = string.Empty;
    public string? CoatTypeCode { get; set; }
    public string? SizeCategoryCode { get; set; }
}
