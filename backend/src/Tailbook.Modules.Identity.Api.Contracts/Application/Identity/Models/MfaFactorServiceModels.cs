namespace Tailbook.Modules.Identity.Application.Identity.Models;

public sealed record MfaFactorView(
    Guid Id,
    string FactorType,
    string Status,
    string TargetEmail,
    DateTimeOffset CreatedAt,
    DateTimeOffset? EnabledAt,
    DateTimeOffset? DisabledAt);
