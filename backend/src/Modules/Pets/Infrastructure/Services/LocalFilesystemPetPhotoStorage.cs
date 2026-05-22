using Microsoft.Extensions.Hosting;

namespace Tailbook.Modules.Pets.Infrastructure.Services;

public sealed class LocalFilesystemPetPhotoStorage(IHostEnvironment hostEnvironment, TimeProvider timeProvider) : IPetPhotoStorage
{
    public async Task<StoredPetPhoto> SaveAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken)
    {
        var safeFileName = Path.GetFileName(fileName);
        var now = timeProvider.GetUtcNow();
        var folder = Path.Combine(hostEnvironment.ContentRootPath, "storage", "pets", now.ToString("yyyy"), now.ToString("MM"));
        Directory.CreateDirectory(folder);

        var finalFileName = $"{Guid.NewGuid():N}_{safeFileName}";
        var fullPath = Path.Combine(folder, finalFileName);

        await using var fileStream = File.Create(fullPath);
        await content.CopyToAsync(fileStream, cancellationToken);

        var storageKey = Path.GetRelativePath(hostEnvironment.ContentRootPath, fullPath).Replace('\\', '/');
        return new StoredPetPhoto(storageKey, safeFileName, contentType);
    }
}
