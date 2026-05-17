using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Messaging;
using Xunit;

namespace Tailbook.BuildingBlocks.Tests;

public sealed class NoOpMessageBrokerTests
{
    private readonly IMessageBroker _broker;

    public NoOpMessageBrokerTests()
    {
        _broker = new NoOpMessageBroker(NullLogger<NoOpMessageBroker>.Instance);
    }

    [Fact]
    public async Task PublishAsync_without_messageId_does_not_throw()
    {
        await _broker.PublishAsync("test.exchange", "test.key", new { data = "value" });
    }

    [Fact]
    public async Task PublishAsync_with_messageId_does_not_throw()
    {
        await _broker.PublishAsync("test.exchange", "test.key", new { data = "value" }, messageId: "msg-1");
    }

    [Fact]
    public async Task PublishAsync_with_cancellationToken_completes()
    {
        using var cts = new CancellationTokenSource();
        await _broker.PublishAsync("e", "k", new { }, cts.Token);
    }

    [Fact]
    public async Task Multiple_publishes_complete_successfully()
    {
        for (var i = 0; i < 10; i++)
        {
            await _broker.PublishAsync("e", $"k.{i}", new { index = i });
        }
    }
}
