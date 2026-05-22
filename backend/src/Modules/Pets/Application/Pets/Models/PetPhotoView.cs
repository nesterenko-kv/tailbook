namespace Tailbook.Modules.Pets.Application.Pets.Models;

public sealed record PetPhotoView(Guid Id, string StorageKey, string FileName, string ContentType, bool IsPrimary, int SortOrder, DateTimeOffset CreatedAt);