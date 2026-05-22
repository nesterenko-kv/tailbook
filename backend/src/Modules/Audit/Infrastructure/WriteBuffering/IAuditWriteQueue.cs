using System.Threading.Channels;

namespace Tailbook.Modules.Audit.Infrastructure.WriteBuffering;

internal interface IAuditWriteQueue
{
    ChannelReader<AuditWriteItem> Reader { get; }

    ValueTask EnqueueAsync(AuditWriteItem item, CancellationToken cancellationToken);
}
