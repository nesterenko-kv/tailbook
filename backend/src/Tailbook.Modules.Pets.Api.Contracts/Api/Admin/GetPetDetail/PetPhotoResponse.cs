namespace Tailbook.Modules.Pets.Api.Admin.GetPetDetail;

public sealed class PetPhotoResponse
{
    public Guid Id { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}