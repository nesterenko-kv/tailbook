using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Tailbook.Modules.Notifications.Application;

public sealed class LocalNotificationSink(IOptions<LocalNotificationOptions> options)
{
    public async Task WriteAsync(object payload, CancellationToken cancellationToken)
    {
        var path = options.Value.LocalSinkPath;
        var fullPath = Path.IsPathRooted(path) ? path : Path.Combine(AppContext.BaseDirectory, path);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        var line = JsonSerializer.Serialize(payload) + Environment.NewLine;
        await File.AppendAllTextAsync(fullPath, line, cancellationToken);
    }
}
