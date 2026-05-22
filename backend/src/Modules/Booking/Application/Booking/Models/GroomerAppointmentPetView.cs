namespace Tailbook.Modules.Booking.Application.Booking.Models;

public sealed record GroomerAppointmentPetView(
    Guid Id,
    string DisplayName,
    string AnimalTypeCode,
    string AnimalTypeName,
    string BreedName,
    string? CoatTypeCode,
    string? SizeCategoryCode);