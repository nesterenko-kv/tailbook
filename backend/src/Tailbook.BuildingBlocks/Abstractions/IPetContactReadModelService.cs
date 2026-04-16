namespace Tailbook.BuildingBlocks.Abstractions;

public interface IPetContactReadModelService
{
    Task<IReadOnlyCollection<PetContactAdminSummary>> GetPetContactsAsync(Guid petId, CancellationToken cancellationToken);
}

public sealed record PetContactAdminSummary(
    Guid ContactId,
    Guid ClientId,
    string FullName,
    bool IsPrimary,
    bool CanPickUp,
    bool CanPay,
    bool ReceivesNotifications,
    IReadOnlyCollection<string> RoleCodes,
    IReadOnlyCollection<ContactMethodAdminSummary> Methods);

public sealed record ContactMethodAdminSummary(
    Guid Id,
    string MethodType,
    string DisplayValue,
    bool IsPreferred,
    string VerificationStatus);
