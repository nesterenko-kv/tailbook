using System.Globalization;
using System.Net.Http.Json;
using Xunit;

namespace Tailbook.Api.Tests.TestSupport.Http;

internal static class ApiClientExtensions
{
    internal static async Task<T> ReadRequiredJsonAsync<T>(this HttpResponseMessage response)
    {
        var payload = await response.Content.ReadFromJsonAsync<T>();
        Assert.NotNull(payload);
        return payload;
    }

    internal static DateTimeOffset UtcDateTime(string value)
        => DateTimeOffset.Parse(value, CultureInfo.InvariantCulture).ToUniversalTime();
}
