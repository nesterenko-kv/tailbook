namespace Tailbook.Modules.Booking.Application.Booking.Models;

public sealed record AppointmentPetView(Guid Id, Guid? ClientId, string AnimalTypeCode, string AnimalTypeName, string BreedName);