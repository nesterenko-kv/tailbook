namespace Tailbook.Modules.Staff.Application.Staff.Queries;

public sealed record CheckGroomerAvailabilityQuery(Guid GroomerId, Guid PetId, DateTimeOffset StartAt, int ReservedMinutes, IReadOnlyCollection<Guid> OfferIds);
