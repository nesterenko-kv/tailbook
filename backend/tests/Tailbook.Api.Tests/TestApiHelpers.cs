namespace Tailbook.Api.Tests;

internal static class TestApiHelpers
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan DefaultPollInterval = TimeSpan.FromMilliseconds(10);

    internal static Task WaitUntilAsync(
        Func<Task<bool>> predicate,
        string failureMessage,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return WaitUntilCoreAsync(
            predicate,
            static (innerPredicate, _) => innerPredicate(),
            failureMessage,
            timeout,
            pollInterval,
            cancellationToken);
    }

    internal static Task WaitUntilAsync(
        Func<CancellationToken, Task<bool>> predicate,
        string failureMessage,
        TimeSpan? timeout = null,
        TimeSpan? pollInterval = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        return WaitUntilCoreAsync(
            predicate,
            static (innerPredicate, token) => innerPredicate(token),
            failureMessage,
            timeout,
            pollInterval,
            cancellationToken);
    }

    private static async Task WaitUntilCoreAsync<TState>(
        TState state,
        Func<TState, CancellationToken, Task<bool>> predicate,
        string failureMessage,
        TimeSpan? timeout,
        TimeSpan? pollInterval,
        CancellationToken cancellationToken)
    {
        var effectiveTimeout = timeout ?? DefaultTimeout;
        var effectivePollInterval = pollInterval ?? DefaultPollInterval;

        if (effectiveTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "Timeout must be greater than zero.");
        }

        if (effectivePollInterval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(pollInterval), pollInterval, "Poll interval must be greater than zero.");
        }

        using var timeoutSource = new CancellationTokenSource(effectiveTimeout);
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutSource.Token);

        try
        {
            while (true)
            {
                linkedSource.Token.ThrowIfCancellationRequested();

                if (await predicate(state, linkedSource.Token).ConfigureAwait(false))
                {
                    return;
                }

                await Task.Delay(effectivePollInterval, linkedSource.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException exception)
            when (timeoutSource.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"{failureMessage} Timed out after {effectiveTimeout}.",
                exception);
        }
    }
}
