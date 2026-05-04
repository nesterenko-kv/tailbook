using Tailbook.BuildingBlocks.Abstractions;

namespace Tailbook.Modules.Customer.Application.Customer.Models;

public sealed record PagedResult<T>(IReadOnlyCollection<T> Items, int Page, int PageSize, int TotalCount);
public sealed record ClientListItemView(Guid Id, string DisplayName, string Status, int ContactCount, DateTime CreatedAtUtc, DateTime UpdatedAtUtc);
public sealed record ClientDetailView(
    Guid Id,
    string DisplayName,
    string Status,
    string? Notes,
    IReadOnlyCollection<ContactPersonView> Contacts,
    IReadOnlyCollection<PetAdminSummary> Pets,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
public sealed record ContactPersonView(Guid Id, Guid ClientId, string FirstName, string? LastName, string? Notes, string TrustLevel, IReadOnlyCollection<ContactMethodView> Methods);
public sealed record ContactMethodView(Guid Id, string MethodType, string DisplayValue, bool IsPreferred, string VerificationStatus, string? Notes);
public sealed record PetContactLinkView(
    Guid PetId,
    Guid ContactId,
    Guid ClientId,
    string ContactDisplayName,
    IReadOnlyCollection<string> RoleCodes,
    bool IsPrimary,
    bool CanPickUp,
    bool CanPay,
    bool ReceivesNotifications,
    IReadOnlyCollection<ContactMethodView> Methods);
