namespace Tailbook.Modules.Customer.Application.Customer.Models;

public sealed record ClientContactPreferencesView(Guid ContactPersonId, Guid ClientId, string FirstName, string? LastName, IReadOnlyCollection<ClientContactMethodPreferenceView> Methods);
public sealed record ClientContactMethodPreferenceView(Guid Id, string MethodType, string DisplayValue, bool IsPreferred, string VerificationStatus, string? Notes);

internal sealed record NormalizedContactMethodInput(string MethodType, string DisplayValue, string NormalizedValue, bool IsPreferred, string? Notes);
