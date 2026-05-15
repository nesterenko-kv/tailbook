namespace Tailbook.Api.Host.Infrastructure;

public sealed class HttpTransportOptions
{
    public const string SectionName = "HttpTransport";

    public bool EnforceHttpsRedirection { get; set; } = true;
    public bool UseHsts { get; set; } = true;
}
