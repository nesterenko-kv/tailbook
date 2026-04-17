using System.Text.Json;
using Microsoft.Extensions.Options;
using Tailbook.Modules.Notifications.Infrastructure;

namespace Tailbook.Modules.Notifications.Application;

public sealed class LocalNotificationSink(IOptions<NotificationsOptions> options)
{
    public async Task WriteAsync(object payload, CancellationToken cancellationToken)
    {
        var path = options.Value.LocalFilePath;
        var fullPath = Path.IsPathRooted(path) ? path : Path.Combine(AppContext.BaseDirectory, path);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        var line = JsonSerializer.Serialize(payload) + Environment.NewLine;
        await File.AppendAllTextAsync(fullPath, line, cancellationToken);
    }
}
