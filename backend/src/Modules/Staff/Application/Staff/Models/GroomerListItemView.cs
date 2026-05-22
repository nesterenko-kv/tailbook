namespace Tailbook.Modules.Staff.Application.Staff.Models;

public sealed record GroomerListItemView(Guid Id, Guid? UserId, string DisplayName, bool Active, int CapabilityCount, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);