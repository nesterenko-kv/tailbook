namespace Tailbook.Modules.Booking.Api.Admin.ListAppointments;

public sealed class ListAppointmentsRequest
{
    public string? Search { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
    public Guid? GroomerId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
