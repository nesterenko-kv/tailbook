using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Customer.Contracts;
using Tailbook.Modules.Customer.Domain;

namespace Tailbook.Modules.Customer.Application;

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

    public async Task<ClientContactPreferencesView?> UpdateContactPreferencesAsync(Guid contactPersonId, UpdateClientContactPreferencesCommand command, CancellationToken cancellationToken)
    {
        var contact = await dbContext.Set<ContactPerson>()
            .SingleOrDefaultAsync(x => x.Id == contactPersonId && x.IsActive, cancellationToken);

        if (contact is null)
        {
            return null;
        }

        var existingMethods = await dbContext.Set<ContactMethod>()
            .Where(x => x.ContactPersonId == contactPersonId && x.IsActive)
            .ToListAsync(cancellationToken);

        var normalizedMethods = command.Methods
            .Where(x => !string.IsNullOrWhiteSpace(x.Value))
            .Select(x => new
            {
                MethodType = NormalizeMethodType(x.MethodType),
                DisplayValue = x.Value.Trim(),
                NormalizedValue = NormalizeContactValue(NormalizeMethodType(x.MethodType), x.Value),
                IsPreferred = x.IsPreferred,
                Notes = NormalizeOptional(x.Notes)
            })
            .ToArray();

        if (normalizedMethods.Length == 0)
        {
            throw new InvalidOperationException("At least one contact method is required.");
        }

        var hasPreferred = normalizedMethods.Any(x => x.IsPreferred);
        for (var index = 0; index < normalizedMethods.Length; index++)
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
        return await GetContactPreferencesAsync(contactPersonId, cancellationToken);
    }

    private static string NormalizeMethodType(string methodType)
    {
        var trimmed = methodType.Trim();
        return trimmed switch
        {
            var x when x.Equals(ContactMethodTypes.Phone, StringComparison.OrdinalIgnoreCase) => ContactMethodTypes.Phone,
            var x when x.Equals(ContactMethodTypes.Instagram, StringComparison.OrdinalIgnoreCase) => ContactMethodTypes.Instagram,
            var x when x.Equals(ContactMethodTypes.Email, StringComparison.OrdinalIgnoreCase) => ContactMethodTypes.Email,
            var x when x.Equals(ContactMethodTypes.Other, StringComparison.OrdinalIgnoreCase) => ContactMethodTypes.Other,
            _ => throw new InvalidOperationException($"Unsupported contact method type '{methodType}'.")
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

public sealed record UpdateClientContactPreferencesCommand(IReadOnlyCollection<UpdateClientContactMethodCommand> Methods);
public sealed record UpdateClientContactMethodCommand(string MethodType, string Value, bool IsPreferred, string? Notes);
public sealed record ClientContactPreferencesView(Guid ContactPersonId, Guid ClientId, string FirstName, string? LastName, IReadOnlyCollection<ClientContactMethodPreferenceView> Methods);
public sealed record ClientContactMethodPreferenceView(Guid Id, string MethodType, string DisplayValue, bool IsPreferred, string VerificationStatus, string? Notes);
