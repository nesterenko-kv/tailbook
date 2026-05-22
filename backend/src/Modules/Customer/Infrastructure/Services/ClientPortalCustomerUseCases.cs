using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;

namespace Tailbook.Modules.Customer.Infrastructure.Services;

public sealed class ClientPortalCustomerUseCases(AppDbContext dbContext, TimeProvider timeProvider) : IClientPortalCustomerReadService
{
    public async Task<ClientContactPreferencesView?> GetContactPreferencesAsync(Guid contactPersonId, CancellationToken cancellationToken)
    {
        var contact = await dbContext.Set<ContactPerson>()
            .SingleOrDefaultAsync(x => x.Id == contactPersonId && x.IsActive, cancellationToken);

        if (contact is null)
        {
            return null;
        }

        var methods = await dbContext.Set<ContactMethod>()
            .Where(x => x.ContactPersonId == contactPersonId && x.IsActive)
            .OrderByDescending(x => x.IsPreferred)
            .ThenBy(x => x.MethodType)
            .Select(x => new ClientContactMethodPreferenceView(x.Id, x.MethodType, x.DisplayValue, x.IsPreferred, x.VerificationStatus, x.Notes))
            .ToListAsync(cancellationToken);

        return new ClientContactPreferencesView(contact.Id, contact.ClientId, contact.FirstName, contact.LastName, methods);
    }

    public async Task<ErrorOr<ClientContactPreferencesView>> UpdateContactPreferencesAsync(Guid contactPersonId, UpdateClientContactPreferencesInput command, CancellationToken cancellationToken)
    {
        var contact = await dbContext.Set<ContactPerson>()
            .Include(x => x.Methods)
            .SingleOrDefaultAsync(x => x.Id == contactPersonId && x.IsActive, cancellationToken);

        if (contact is null)
        {
            return Error.NotFound("Customer.ContactNotFound", "Contact does not exist.");
        }

        var utcNow = timeProvider.GetUtcNow();

        var normalizedMethods = new List<(string MethodType, string RawValue, string DisplayValue, bool IsPreferred, string? Notes)>();
        foreach (var method in command.Methods.Where(x => !string.IsNullOrWhiteSpace(x.Value)))
        {
            var normalizedMethodType = NormalizeMethodType(method.MethodType);
            if (normalizedMethodType.IsError)
            {
                return normalizedMethodType.Errors;
            }

            normalizedMethods.Add((
                normalizedMethodType.Value,
                method.Value,
                method.Value.Trim(),
                method.IsPreferred,
                method.Notes));
        }

        if (normalizedMethods.Count == 0)
        {
            return Error.Validation("Customer.ContactMethodRequired", "At least one contact method is required.");
        }

        var hasPreferred = normalizedMethods.Any(x => x.IsPreferred);

        contact.ClearPreferredMethods(utcNow);

        for (var index = 0; index < normalizedMethods.Count; index++)
        {
            var (methodType, rawValue, displayValue, isPreferred, notes) = normalizedMethods[index];
            var effectiveIsPreferred = hasPreferred ? isPreferred : index == 0;

            var existing = contact.GetMethod(methodType, rawValue);
            if (existing is null)
            {
                contact.AddContactMethod(methodType, rawValue, displayValue, effectiveIsPreferred, ContactVerificationStatuses.Unverified, utcNow);
            }
            else
            {
                existing.UpdateDetails(displayValue, notes, utcNow);
                existing.SetPreferred(effectiveIsPreferred, utcNow);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return (await GetContactPreferencesAsync(contactPersonId, cancellationToken))!;
    }

    private static ErrorOr<string> NormalizeMethodType(string methodType)
    {
        var trimmed = methodType.Trim();
        return trimmed switch
        {
            var x when x.Equals(ContactMethodTypes.Phone, StringComparison.OrdinalIgnoreCase) => ContactMethodTypes.Phone,
            var x when x.Equals(ContactMethodTypes.Instagram, StringComparison.OrdinalIgnoreCase) => ContactMethodTypes.Instagram,
            var x when x.Equals(ContactMethodTypes.Email, StringComparison.OrdinalIgnoreCase) => ContactMethodTypes.Email,
            var x when x.Equals(ContactMethodTypes.Other, StringComparison.OrdinalIgnoreCase) => ContactMethodTypes.Other,
            _ => Error.Validation("Customer.UnsupportedContactMethodType", $"Unsupported contact method type '{methodType}'.")
        };
    }

}
