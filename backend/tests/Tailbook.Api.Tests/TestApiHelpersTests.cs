using System.Diagnostics;
using Tailbook.BuildingBlocks.Infrastructure.Diagnostics;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class TestApiHelpersTests
{
    [Fact]
    public async Task WaitUntilAsync_cancels_predicate_when_timeout_elapses()
    {
        var stopwatch = ValueStopwatch.StartNew();

        var exception = await Assert.ThrowsAsync<TimeoutException>(() =>
            TestApiHelpers.WaitUntilAsync(
                async cancellationToken =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                    return false;
                },
                "Wait timed out.",
                TimeSpan.FromMilliseconds(20)));

        Assert.Multiple(
            () => Assert.Contains("Wait timed out.", exception.Message),
            () => Assert.True(stopwatch.GetElapsedTime() < TimeSpan.FromSeconds(2))
        );
    }

    [Fact]
    public async Task WaitUntilAsync_propagates_external_cancellation()
    {
        using var cancellationSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(20));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            TestApiHelpers.WaitUntilAsync(
                async cancellationToken =>
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                    return false;
                },
                "Wait timed out.",
                TimeSpan.FromSeconds(5),
                null,
                cancellationSource.Token));
    }
}
