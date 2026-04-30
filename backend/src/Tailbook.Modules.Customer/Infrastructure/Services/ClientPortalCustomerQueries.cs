using System.Text.RegularExpressions;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Customer.Contracts;

namespace Tailbook.Modules.Customer.Infrastructure.Services;

public sealed class ClientPortalCustomerQueries(AppDbContext dbContext)
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

    public async Task<ErrorOr<ClientContactPreferencesView>> UpdateContactPreferencesAsync(Guid contactPersonId, UpdateClientContactPreferencesCommand command, CancellationToken cancellationToken)
    {
        var contact = await dbContext.Set<ContactPerson>()
            .SingleOrDefaultAsync(x => x.Id == contactPersonId && x.IsActive, cancellationToken);

        if (contact is null)
        {
            return Error.NotFound("Customer.ContactNotFound", "Contact does not exist.");
        }

        var existingMethods = await dbContext.Set<ContactMethod>()
            .Where(x => x.ContactPersonId == contactPersonId && x.IsActive)
            .ToListAsync(cancellationToken);

        var normalizedMethods = new List<NormalizedContactMethodInput>();
        foreach (var method in command.Methods.Where(x => !string.IsNullOrWhiteSpace(x.Value)))
        {
            var normalizedMethodType = NormalizeMethodType(method.MethodType);
            if (normalizedMethodType.IsError)
            {
                return normalizedMethodType.Errors;
            }

            normalizedMethods.Add(new NormalizedContactMethodInput(
                normalizedMethodType.Value,
                method.Value.Trim(),
                NormalizeContactValue(normalizedMethodType.Value, method.Value),
                method.IsPreferred,
                NormalizeOptional(method.Notes)));
        }

        if (normalizedMethods.Count == 0)
        {
            return Error.Validation("Customer.ContactMethodRequired", "At least one contact method is required.");
        }

        var hasPreferred = normalizedMethods.Any(x => x.IsPreferred);
        for (var index = 0; index < normalizedMethods.Count; index++)
        {
            var item = normalizedMethods[index];
            var isPreferred = hasPreferred ? item.IsPreferred : index == 0;
            var existing = existingMethods.SingleOrDefault(x => x.MethodType == item.MethodType && x.NormalizedValue == item.NormalizedValue);
            if (existing is null)
            {
                existing = new ContactMethod
                {
                    Id = Guid.NewGuid(),
                    ContactPersonId = contactPersonId,
                    MethodType = item.MethodType,
                    NormalizedValue = item.NormalizedValue,
                    DisplayValue = item.DisplayValue,
                    IsPreferred = isPreferred,
                    VerificationStatus = ContactVerificationStatuses.Unverified,
                    IsActive = true,
                    Notes = item.Notes,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                };

                dbContext.Set<ContactMethod>().Add(existing);
                existingMethods.Add(existing);
            }
            else
            {
                existing.DisplayValue = item.DisplayValue;
                existing.IsPreferred = isPreferred;
                existing.Notes = item.Notes;
                existing.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        if (hasPreferred)
        {
            var preferredKeys = normalizedMethods.Where(x => x.IsPreferred).Select(x => (x.MethodType, x.NormalizedValue)).ToHashSet();
            foreach (var method in existingMethods)
            {
                method.IsPreferred = preferredKeys.Contains((method.MethodType, method.NormalizedValue));
                method.UpdatedAtUtc = DateTime.UtcNow;
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

    private static string NormalizeContactValue(string methodType, string value)
    {
        var trimmed = value.Trim();
        return methodType switch
        {
            ContactMethodTypes.Phone => Regex.Replace(trimmed, "[^0-9+]", string.Empty),
            ContactMethodTypes.Instagram => trimmed.TrimStart('@').ToLowerInvariant(),
            ContactMethodTypes.Email => trimmed.ToLowerInvariant(),
            _ => trimmed
        };
    }

    private static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
