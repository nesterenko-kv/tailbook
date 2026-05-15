using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using Tailbook.Modules.Notifications.Infrastructure.Options;

namespace Tailbook.Modules.Notifications.Infrastructure.Services;

public sealed class SmtpNotificationSink(IOptions<NotificationsOptions> options) : INotificationSink
{
    public async Task SendAsync(NotificationDispatchEnvelope envelope, CancellationToken cancellationToken)
    {
        var configuration = options.Value;

        try
        {
            using var message = CreateMessage(envelope, configuration);
            using var client = CreateClient(configuration);

            await client.SendMailAsync(message, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (FormatException ex)
        {
            throw new NotificationDeliveryException("SMTP delivery failed because email addressing was invalid.", ex);
        }
        catch (InvalidOperationException ex)
        {
            throw new NotificationDeliveryException("SMTP delivery failed because SMTP configuration was invalid.", ex);
        }
        catch (SmtpException ex)
        {
            throw new NotificationDeliveryException($"SMTP delivery failed with status {ex.StatusCode}.", ex);
        }
    }

    private static MailMessage CreateMessage(NotificationDispatchEnvelope envelope, NotificationsOptions options)
    {
        var message = new MailMessage
        {
            From = string.IsNullOrWhiteSpace(options.SmtpFromName)
                ? new MailAddress(options.SmtpFromEmail)
                : new MailAddress(options.SmtpFromEmail, options.SmtpFromName),
            Subject = envelope.Subject,
            Body = envelope.Body,
            IsBodyHtml = false
        };
        message.To.Add(new MailAddress(envelope.Recipient));
        return message;
    }

    private static SmtpClient CreateClient(NotificationsOptions options)
    {
        var client = new SmtpClient(options.SmtpHost, options.SmtpPort)
        {
            EnableSsl = options.SmtpEnableSsl,
            Timeout = checked(options.SmtpTimeoutSeconds * 1000),
            UseDefaultCredentials = false
        };

        if (!string.IsNullOrWhiteSpace(options.SmtpUsername) || !string.IsNullOrWhiteSpace(options.SmtpPassword))
        {
            client.Credentials = new NetworkCredential(options.SmtpUsername, options.SmtpPassword);
        }

        return client;
    }
}
