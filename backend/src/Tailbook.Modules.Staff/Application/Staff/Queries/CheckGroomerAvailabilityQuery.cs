namespace Tailbook.Modules.Staff.Application.Staff.Queries;

public sealed record CheckGroomerAvailabilityQuery(Guid GroomerId, Guid PetId, DateTime StartAtUtc, int ReservedMinutes, IReadOnlyCollection<Guid> OfferIds);
