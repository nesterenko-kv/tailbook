namespace Tailbook.Modules.Booking.Api.Contracts.Admin.BulkCancelAppointments;

public sealed class BulkCancelAppointmentsResponse
{
    public int Succeeded { get; set; }
    public int Failed { get; set; }
    public List<BulkCancelAppointmentErrorItem> Errors { get; set; } = [];
}

public sealed class BulkCancelAppointmentErrorItem
{
    public string AppointmentId { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}
