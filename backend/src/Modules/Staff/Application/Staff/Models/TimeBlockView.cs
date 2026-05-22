namespace Tailbook.Modules.Staff.Application.Staff.Models;

public sealed record TimeBlockView(Guid Id, Guid GroomerId, DateTimeOffset StartAt, DateTimeOffset EndAt, string ReasonCode, string? Notes, DateTimeOffset CreatedAt);