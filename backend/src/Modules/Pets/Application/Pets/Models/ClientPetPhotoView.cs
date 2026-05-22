namespace Tailbook.Modules.Pets.Application.Pets.Models;

public sealed record ClientPetPhotoView(Guid Id, string FileName, string ContentType, bool IsPrimary, int SortOrder);