using System.Globalization;
using System.Net.Http.Json;
using Xunit;

namespace Tailbook.Api.Tests.TestSupport.Http;

public static class ApiClientExtensions
{
    public static async Task<T> ReadRequiredJsonAsync<T>(this HttpResponseMessage response)
    {
        var payload = await response.Content.ReadFromJsonAsync<T>();
        Assert.NotNull(payload);
        return payload;
    }

    public static DateTimeOffset UtcDateTime(string value)
        => DateTimeOffset.Parse(value, CultureInfo.InvariantCulture).ToUniversalTime();
}
