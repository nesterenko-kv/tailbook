namespace Tailbook.Modules.VisitOperations.Api.Admin.ListVisits;

public sealed class ListVisitsRequest
{
    public string? Search { get; set; }
    public string? VisitStatus { get; set; }

    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
    public Guid? GroomerId { get; set; }
    public Guid? AppointmentId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
