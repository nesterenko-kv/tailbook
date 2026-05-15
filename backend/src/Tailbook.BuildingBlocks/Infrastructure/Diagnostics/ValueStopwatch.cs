using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Tailbook.BuildingBlocks.Infrastructure.Diagnostics;

/// <summary>
///     This is already familiar <see cref="Stopwatch" /> but readonly struct.
///     Doesn't allocate memory on the managed heap.
/// </summary>
[SuppressMessage("ReSharper", "UnusedMember.Global")]
[SuppressMessage("ReSharper", "UnusedType.Global")]
public readonly struct ValueStopwatch
{
    private static readonly double TimestampToTicks = TimeSpan.TicksPerSecond / (double)Stopwatch.Frequency;
    private const long TicksPerMillisecond = TimeSpan.TicksPerMillisecond;
    private const long MaxMilliseconds = long.MaxValue / TicksPerMillisecond;
    private const long MinMilliseconds = long.MinValue / TicksPerMillisecond;

    private readonly long _startTimestamp;

    /// <summary>
    ///     Gets a value indicating whether the stopwatch is running.
    /// </summary>
    // Start timestamp can't be zero in an initialized ValueStopwatch.
    // It would have to be literally the first thing executed when the machine boots to be 0.
    // So it being 0 is a clear indication of default(ValueStopwatch)
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public bool IsActive => _startTimestamp != 0;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ValueStopwatch" /> structure.
    /// </summary>
    /// <param name="startTimestamp">The starting timestamp for the stopwatch.</param>
    private ValueStopwatch(
        long startTimestamp
    )
    {
        _startTimestamp = startTimestamp;
    }

    /// <summary>
    ///     Starts a new instance of the <see cref="ValueStopwatch" /> structure.
    /// </summary>
    /// <returns>A new instance of the <see cref="ValueStopwatch" /> structure.</returns>
    public static ValueStopwatch StartNew()
    {
        return new ValueStopwatch(Stopwatch.GetTimestamp());
    }

    /// <summary>
    ///     Gets the number of elapsed ticks.
    /// </summary>
    /// <returns>The number of elapsed ticks.</returns>
    public long GetElapsedTicks()
    {
        AssertIsActive();

        var ticks = GetElapsedTicksInternal();

        return ticks;
    }

    /// <summary>
    ///     Gets the elapsed time.
    /// </summary>
    /// <returns>The elapsed time.</returns>
    public TimeSpan GetElapsedTime()
    {
        AssertIsActive();

        return Stopwatch.GetElapsedTime(_startTimestamp);
    }

    /// <summary>
    ///     Gets the number of elapsed milliseconds.
    /// </summary>
    /// <returns>The elapsed milliseconds.</returns>
    public double GetElapsedMilliseconds()
    {
        AssertIsActive();

        var ticks = GetElapsedTicksInternal();
        var temp = (double)ticks / TicksPerMillisecond;

        return Math.Clamp(temp, MinMilliseconds, MaxMilliseconds);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AssertIsActive()
    {
        if (!IsActive)
            throw new InvalidOperationException(
                "An uninitialized, or 'default', ValueStopwatch cannot be used to get elapsed time."
            );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private long GetElapsedTicksInternal()
    {
        var end = Stopwatch.GetTimestamp();
        var timestampDelta = end - _startTimestamp;
        var ticks = (long)(TimestampToTicks * timestampDelta);
        return ticks;
    }
}
