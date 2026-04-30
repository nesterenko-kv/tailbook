namespace Tailbook.Modules.Identity.Application.Identity.Models;

public sealed record MfaFactorView(
    Guid Id,
    string FactorType,
    string Status,
    string TargetEmail,
    DateTime CreatedAtUtc,
    DateTime? EnabledAtUtc,
    DateTime? DisabledAtUtc);
