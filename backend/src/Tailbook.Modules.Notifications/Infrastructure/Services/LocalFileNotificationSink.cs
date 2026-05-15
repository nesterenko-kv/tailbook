using System.Text.Json;
using Microsoft.Extensions.Options;
using Tailbook.Modules.Notifications.Infrastructure.Options;

namespace Tailbook.Modules.Notifications.Infrastructure.Services;

public sealed class LocalFileNotificationSink(IOptions<NotificationsOptions> options) : INotificationSink
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task SendAsync(NotificationDispatchEnvelope envelope, CancellationToken cancellationToken)
    {
        var configuredPath = options.Value.LocalFilePath;
        var filePath = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(AppContext.BaseDirectory, configuredPath);

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var line = JsonSerializer.Serialize(envelope, JsonOptions) + Environment.NewLine;
        await File.AppendAllTextAsync(filePath, line, cancellationToken);
    }
}
