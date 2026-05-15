namespace Tailbook.Modules.Pets.Application.Abstractions.Services;

public interface IPetPhotoStorage
{
    Task<StoredPetPhoto> SaveAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken);
}