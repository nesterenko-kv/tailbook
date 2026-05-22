namespace Tailbook.Modules.Booking.Api.Admin.CreateAppointment;

public sealed class CreateAppointmentItemPayload
{
    public Guid OfferId { get; set; }
    public string? ItemType { get; set; }
}