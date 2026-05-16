using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tailbook.Api.Host.Infrastructure;

public sealed class UtcDateTimeOffsetJsonConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Expected a date-time string.");
        }

        var value = reader.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new JsonException("Expected a non-empty date-time string.");
        }

        if (!HasExplicitOffset(value))
        {
            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var dateTime))
            {
                return new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Unspecified), TimeSpan.Zero);
            }

            throw new JsonException("Invalid date-time string.");
        }

        return DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).ToUniversalTime();
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.UtcDateTime);
    }

    private static bool HasExplicitOffset(string value)
    {
        var trimmed = value.AsSpan().Trim();
        if (trimmed.Length == 0)
        {
            return false;
        }

        if (trimmed[^1] is 'Z' or 'z')
        {
            return true;
        }

        var separatorIndex = trimmed.IndexOf('T');
        if (separatorIndex < 0)
        {
            separatorIndex = trimmed.IndexOf('t');
        }

        if (separatorIndex < 0)
        {
            separatorIndex = trimmed.IndexOf(' ');
        }

        var plusIndex = trimmed.LastIndexOf('+');
        var minusIndex = trimmed.LastIndexOf('-');
        var signIndex = plusIndex > minusIndex ? plusIndex : minusIndex;
        return separatorIndex >= 0 && signIndex > separatorIndex;
    }
}
