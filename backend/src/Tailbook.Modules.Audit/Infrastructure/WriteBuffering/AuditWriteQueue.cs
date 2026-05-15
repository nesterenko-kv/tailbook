using System.Threading.Channels;
using Microsoft.Extensions.Options;
using Tailbook.BuildingBlocks.Infrastructure.Diagnostics;
using Tailbook.Modules.Audit.Infrastructure.Telemetry;

namespace Tailbook.Modules.Audit.Infrastructure.WriteBuffering;

internal sealed class AuditWriteQueue : IAuditWriteQueue
{
    private readonly Channel<AuditWriteItem> _channel;

    public AuditWriteQueue(IOptions<AuditWriteOptions> optionsAccessor)
    {
        ArgumentNullException.ThrowIfNull(optionsAccessor);

        var options = optionsAccessor.Value;
        var channelOptions = new BoundedChannelOptions(options.QueueCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        };

        _channel = Channel.CreateBounded<AuditWriteItem>(channelOptions);
    }

    public ChannelReader<AuditWriteItem> Reader => _channel.Reader;

    public async ValueTask EnqueueAsync(AuditWriteItem item, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);
        cancellationToken.ThrowIfCancellationRequested();

        var stopwatch = ValueStopwatch.StartNew();
        var itemType = AuditWriteItemTypes.GetTelemetryItemType(item);

        try
        {
            await _channel.Writer.WriteAsync(item, cancellationToken);
            AuditTelemetry.RecordQueueEnqueued(itemType, stopwatch.GetElapsedTime(), AuditTelemetry.ResultAccepted);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            AuditTelemetry.RecordQueueEnqueued(itemType, stopwatch.GetElapsedTime(), AuditTelemetry.ResultCanceled);
            throw;
        }
        catch
        {
            AuditTelemetry.RecordQueueEnqueued(itemType, stopwatch.GetElapsedTime(), AuditTelemetry.ResultError);
            throw;
        }
    }
}
