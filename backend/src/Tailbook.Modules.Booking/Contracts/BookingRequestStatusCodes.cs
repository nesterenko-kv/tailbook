namespace Tailbook.Modules.Booking.Contracts;

public static class BookingRequestStatusCodes
{
    public const string Submitted = "Submitted";
    public const string NeedsReview = "NeedsReview";
    public const string Converted = "Converted";
    public const string Rejected = "Rejected";
    public const string Expired = "Expired";

    public static readonly string[] All =
    [
        Submitted,
        NeedsReview,
        Converted,
        Rejected,
        Expired
    ];
}
