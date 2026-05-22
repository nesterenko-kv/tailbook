namespace Tailbook.Modules.Pets.Application.Abstractions.Services;

public sealed record StoredPetPhoto(string StorageKey, string FileName, string ContentType);