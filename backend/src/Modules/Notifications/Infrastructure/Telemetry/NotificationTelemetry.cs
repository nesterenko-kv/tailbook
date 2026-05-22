using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Tailbook.Modules.Notifications.Infrastructure.Telemetry;

public static class NotificationTelemetry
{
    public const string ActivitySourceName = "Tailbook.Notifications";
    public const string MeterName = "Tailbook.Notifications";
    public const string NotificationProcessingActivityName = "notifications.process";
    public const string TriggerManual = "manual";
    public const string TriggerBackground = "background";
    public const string ResultProcessed = "processed";
    public const string ResultIdle = "idle";
    public const string ResultSkipped = "skipped";
    public const string ResultError = "error";
    public const string ResultCanceled = "canceled";
    public const string OutcomeIgnored = "ignored";
    public const string OutcomeAlreadyFinal = "already_final";
    public const string OutcomeSkippedRetry = "skipped_retry";
    public const string OutcomeSent = "sent";
    public const string OutcomeFailed = "failed";
    public const string OutcomeDeadLetter = "dead_letter";

    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);
    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> NotificationProcessingCycles = Meter.CreateCounter<long>(
        "tailbook.notifications.processing.cycles",
        description: "Notification processing cycles.");
    private static readonly Counter<long> NotificationsProcessed = Meter.CreateCounter<long>(
        "tailbook.notifications.processed",
        description: "Notifications processed by cycle result.");
    private static readonly Counter<long> NotificationOutcomes = Meter.CreateCounter<long>(
        "tailbook.notifications.outcomes",
        description: "Notifications handled by outcome.");
    private static readonly Counter<long> DeliveryAttempts = Meter.CreateCounter<long>(
        "tailbook.notifications.delivery.attempts",
        description: "Notification delivery attempts by status and channel.");
    private static readonly Counter<long> BackgroundProcessingFailures = Meter.CreateCounter<long>(
        "tailbook.notifications.background.failures",
        description: "Unhandled notification background processor failures.");
    private static readonly Histogram<double> NotificationProcessingDuration = Meter.CreateHistogram<double>(
        "tailbook.notifications.processing.duration",
        "ms",
        "Notification processing cycle duration.");

    public static Activity? StartNotificationProcessingActivity(string trigger)
    {
        var activity = ActivitySource.StartActivity(NotificationProcessingActivityName);
        activity?.SetTag("tailbook.notifications.trigger", Normalize(trigger));
        return activity;
    }

    public static void SetPendingNotificationCount(Activity? activity, int pendingCount)
    {
        activity?.SetTag("tailbook.notifications.pending_notification_count", pendingCount);
    }

    public static void SetNotificationProcessingCounts(
        Activity? activity,
        int processedCount,
        int sentCount,
        int failedCount,
        int deadLetterCount,
        int ignoredCount,
        int skippedRetryCount)
    {
        activity?.SetTag("tailbook.notifications.processed_count", processedCount);
        activity?.SetTag("tailbook.notifications.sent_count", sentCount);
        activity?.SetTag("tailbook.notifications.failed_count", failedCount);
        activity?.SetTag("tailbook.notifications.dead_letter_count", deadLetterCount);
        activity?.SetTag("tailbook.notifications.ignored_count", ignoredCount);
        activity?.SetTag("tailbook.notifications.skipped_retry_count", skippedRetryCount);
    }

    public static void SetNotificationProcessingResult(Activity? activity, string result, TimeSpan duration)
    {
        activity?.SetTag("tailbook.notifications.result", Normalize(result));
        activity?.SetTag("tailbook.notifications.duration_ms", duration.TotalMilliseconds);
    }

    public static void RecordNotificationProcessingCycle(string trigger, int processedCount, TimeSpan duration, string result)
    {
        var tags = new TagList
        {
            { "tailbook.notifications.trigger", Normalize(trigger) },
            { "tailbook.notifications.result", Normalize(result) }
        };

        NotificationProcessingCycles.Add(1, tags);
        NotificationProcessingDuration.Record(duration.TotalMilliseconds, tags);

        if (processedCount > 0)
        {
            NotificationsProcessed.Add(processedCount, tags);
        }
    }

    public static void RecordNotificationOutcome(string outcome)
    {
        var tags = new TagList
        {
            { "tailbook.notifications.outcome", Normalize(outcome) }
        };

        NotificationOutcomes.Add(1, tags);
    }

    public static void RecordDeliveryAttempt(string status, string channel)
    {
        var tags = new TagList
        {
            { "tailbook.notifications.status", Normalize(status) },
            { "tailbook.notifications.channel", Normalize(channel) }
        };

        DeliveryAttempts.Add(1, tags);
    }

    public static void RecordBackgroundProcessingFailure()
    {
        BackgroundProcessingFailures.Add(1);
    }

    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim();
    }
}
