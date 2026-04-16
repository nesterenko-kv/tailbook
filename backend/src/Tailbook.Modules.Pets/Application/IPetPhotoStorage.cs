namespace Tailbook.Modules.Pets.Application;

public interface IPetPhotoStorage
{
    Task<StoredPetPhoto> SaveAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken);
}

public sealed record StoredPetPhoto(string StorageKey, string FileName, string ContentType);
