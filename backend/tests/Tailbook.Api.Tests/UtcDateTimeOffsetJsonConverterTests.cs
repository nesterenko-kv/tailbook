using System.Text.Json;
using Tailbook.Api.Host.Infrastructure;
using Xunit;

namespace Tailbook.Api.Tests;

public sealed class UtcDateTimeOffsetJsonConverterTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new UtcDateTimeOffsetJsonConverter() }
    };

    [Fact]
    public void Read_treats_missing_offset_as_utc_wall_clock()
    {
        var value = JsonSerializer.Deserialize<DateTimeOffset>("\"2026-04-22T07:00:00\"", Options);

        Assert.Equal(TimeSpan.Zero, value.Offset);
        Assert.Equal(new DateTimeOffset(2026, 4, 22, 7, 0, 0, TimeSpan.Zero), value);
    }

    [Fact]
    public void Read_normalizes_explicit_offsets_to_utc()
    {
        var value = JsonSerializer.Deserialize<DateTimeOffset>("\"2026-04-22T10:00:00+03:00\"", Options);

        Assert.Equal(TimeSpan.Zero, value.Offset);
        Assert.Equal(new DateTimeOffset(2026, 4, 22, 7, 0, 0, TimeSpan.Zero), value);
    }

    [Fact]
    public void Write_emits_utc_offset()
    {
        var json = JsonSerializer.Serialize(new DateTimeOffset(2026, 4, 22, 10, 0, 0, TimeSpan.FromHours(3)), Options);

        Assert.Equal("\"2026-04-22T07:00:00Z\"", json);
    }
}
