namespace Tailbook.Modules.Pets.Domain.Entities;

public sealed class PetPhoto
{
    public Guid Id { get; set; }
    public Guid PetId { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
