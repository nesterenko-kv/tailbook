namespace Tailbook.Modules.Booking.Api.Public;

public sealed class PublicRequesterPayload
{
    public string? DisplayName { get; set; }
    public string? Phone { get; set; }
    public string? InstagramHandle { get; set; }
    public string? Email { get; set; }
    public string? PreferredContactMethodCode { get; set; }
}