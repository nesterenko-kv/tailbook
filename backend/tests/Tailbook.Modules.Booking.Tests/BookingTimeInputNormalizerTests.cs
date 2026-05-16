using Xunit;

namespace Tailbook.Modules.Booking.Tests;

public sealed class BookingTimeInputNormalizerTests
{
    [Fact]
    public void AssumeUtc_returns_error_for_default_value()
    {
        var result = BookingTimeInputNormalizer.AssumeUtc(default, "testParam");

        Assert.True(result.IsError);
    }

    [Fact]
    public void AssumeUtc_converts_to_utc()
    {
        var localTime = new DateTimeOffset(2026, 4, 22, 10, 0, 0, TimeSpan.FromHours(3));
        var result = BookingTimeInputNormalizer.AssumeUtc(localTime, "test");

        Assert.False(result.IsError);
        Assert.Equal(TimeSpan.Zero, result.Value.Offset);
        Assert.Equal(7, result.Value.Hour); // 10:00 +03:00 -> 07:00 UTC
    }

    [Fact]
    public void AssumeUtc_preserves_utc_value()
    {
        var utcTime = new DateTimeOffset(2026, 4, 22, 10, 0, 0, TimeSpan.Zero);
        var result = BookingTimeInputNormalizer.AssumeUtc(utcTime, "test");

        Assert.False(result.IsError);
        Assert.Equal(utcTime, result.Value);
    }

    [Fact]
    public void CreatePeriod_returns_error_for_default_start()
    {
        var end = new DateTimeOffset(2026, 4, 22, 10, 0, 0, TimeSpan.Zero);
        var result = BookingTimeInputNormalizer.CreatePeriod(default, end);

        Assert.True(result.IsError);
    }

    [Fact]
    public void CreatePeriod_returns_error_for_default_end()
    {
        var start = new DateTimeOffset(2026, 4, 22, 8, 0, 0, TimeSpan.Zero);
        var result = BookingTimeInputNormalizer.CreatePeriod(start, default);

        Assert.True(result.IsError);
    }

    [Fact]
    public void CreatePeriod_returns_error_when_end_before_start()
    {
        var start = new DateTimeOffset(2026, 4, 22, 10, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2026, 4, 22, 8, 0, 0, TimeSpan.Zero);
        var result = BookingTimeInputNormalizer.CreatePeriod(start, end);

        Assert.True(result.IsError);
    }

    [Fact]
    public void CreatePeriod_returns_period_for_valid_input()
    {
        var start = new DateTimeOffset(2026, 4, 22, 8, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2026, 4, 22, 10, 0, 0, TimeSpan.Zero);
        var result = BookingTimeInputNormalizer.CreatePeriod(start, end);

        Assert.False(result.IsError);
        Assert.Equal(start, result.Value.StartAt);
        Assert.Equal(end, result.Value.EndAt);
    }

    [Fact]
    public void CreatePeriod_normalizes_offsets()
    {
        var start = new DateTimeOffset(2026, 4, 22, 10, 0, 0, TimeSpan.FromHours(2));
        var end = new DateTimeOffset(2026, 4, 22, 14, 0, 0, TimeSpan.FromHours(2));
        var result = BookingTimeInputNormalizer.CreatePeriod(start, end);

        Assert.False(result.IsError);
        Assert.Equal(TimeSpan.Zero, result.Value.StartAt.Offset);
        Assert.Equal(TimeSpan.Zero, result.Value.EndAt.Offset);
    }
}