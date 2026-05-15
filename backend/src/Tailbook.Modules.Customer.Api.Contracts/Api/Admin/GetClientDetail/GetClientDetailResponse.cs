namespace Tailbook.Modules.Customer.Api.Admin.GetClientDetail;

public sealed class GetClientDetailResponse
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public IReadOnlyCollection<ContactPersonResponse> Contacts { get; set; } = [];
    public IReadOnlyCollection<ClientPetSummaryResponse> Pets { get; set; } = [];
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
