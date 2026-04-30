using System.Text.RegularExpressions;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Customer.Contracts;

namespace Tailbook.Modules.Customer.Infrastructure.Services;

public sealed class CustomerQueries(
    AppDbContext dbContext,
    IPetReadModelService petReadModelService,
    IPetReferenceValidationService petReferenceValidationService,
    IAccessAuditService accessAuditService) : ICustomerQueries
{
    public async Task<PagedResult<ClientListItemView>> ListClientsAsync(string? search, int page, int pageSize, CancellationToken cancellationToken)
    {
        var safePage = page <= 0 ? 1 : page;
        var safePageSize = pageSize switch
        {
            <= 0 => 20,
            > 100 => 100,
            _ => pageSize
        };

        var query = dbContext.Set<Client>().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim();
            query = query.Where(x => EF.Functions.ILike(x.DisplayName, $"%{normalizedSearch}%"));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var clients = await query
            .OrderBy(x => x.DisplayName)
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToListAsync(cancellationToken);

        var clientIds = clients.Select(x => x.Id).ToArray();
        var contactCounts = await dbContext.Set<ContactPerson>()
            .Where(x => clientIds.Contains(x.ClientId) && x.IsActive)
            .GroupBy(x => x.ClientId)
            .Select(x => new { ClientId = x.Key, Count = x.Count() })
            .ToListAsync(cancellationToken);

        var items = clients.Select(x => new ClientListItemView(
            x.Id,
            x.DisplayName,
            x.Status,
            contactCounts.SingleOrDefault(y => y.ClientId == x.Id)?.Count ?? 0,
            x.CreatedAtUtc,
            x.UpdatedAtUtc)).ToArray();

        return new PagedResult<ClientListItemView>(items, safePage, safePageSize, totalCount);
    }

    public async Task<ClientDetailView?> GetClientDetailAsync(Guid clientId, Guid? actorUserId, CancellationToken cancellationToken)
    {
        var client = await dbContext.Set<Client>().SingleOrDefaultAsync(x => x.Id == clientId, cancellationToken);
        if (client is null)
        {
            return null;
        }

        var contacts = await dbContext.Set<ContactPerson>()
            .Where(x => x.ClientId == clientId && x.IsActive)
            .OrderBy(x => x.FirstName)
            .ThenBy(x => x.LastName)
            .ToListAsync(cancellationToken);

        var contactIds = contacts.Select(x => x.Id).ToArray();
        var methods = await dbContext.Set<ContactMethod>()
            .Where(x => contactIds.Contains(x.ContactPersonId) && x.IsActive)
            .OrderByDescending(x => x.IsPreferred)
            .ThenBy(x => x.MethodType)
            .ToListAsync(cancellationToken);

        var pets = await petReadModelService.GetPetsByClientAsync(clientId, cancellationToken);

        if (actorUserId.HasValue)
        {
            await accessAuditService.RecordAsync("crm_client", clientId.ToString("D"), "READ_CONTACT_DATA", actorUserId, cancellationToken);
        }

        return new ClientDetailView(
            client.Id,
            client.DisplayName,
            client.Status,
            client.Notes,
            contacts.Select(contact => new ContactPersonView(
                contact.Id,
                contact.ClientId,
                contact.FirstName,
                contact.LastName,
                contact.Notes,
                contact.TrustLevel,
                methods.Where(x => x.ContactPersonId == contact.Id)
                    .Select(x => new ContactMethodView(x.Id, x.MethodType, x.DisplayValue, x.IsPreferred, x.VerificationStatus, x.Notes))
                    .ToArray()))
                .ToArray(),
            pets,
            client.CreatedAtUtc,
            client.UpdatedAtUtc);
    }

    public async Task<ClientDetailView> CreateClientAsync(string displayName, string? notes, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var client = new Client
        {
            Id = Guid.NewGuid(),
            DisplayName = displayName.Trim(),
            Status = ClientStatusCodes.Active,
            Notes = NormalizeOptional(notes),
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };

        dbContext.Set<Client>().Add(client);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ClientDetailView(client.Id, client.DisplayName, client.Status, client.Notes, [], [], client.CreatedAtUtc, client.UpdatedAtUtc);
    }

    public async Task<ContactPersonView?> AddContactPersonAsync(Guid clientId, string firstName, string? lastName, string? notes, string? trustLevel, CancellationToken cancellationToken)
    {
        var clientExists = await dbContext.Set<Client>().AnyAsync(x => x.Id == clientId, cancellationToken);
        if (!clientExists)
        {
            return null;
        }

        var utcNow = DateTime.UtcNow;
        var contact = new ContactPerson
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            FirstName = firstName.Trim(),
            LastName = NormalizeOptional(lastName),
            Notes = NormalizeOptional(notes),
            TrustLevel = string.IsNullOrWhiteSpace(trustLevel) ? ContactTrustLevels.Standard : trustLevel.Trim(),
            IsActive = true,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };

        dbContext.Set<ContactPerson>().Add(contact);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ContactPersonView(contact.Id, contact.ClientId, contact.FirstName, contact.LastName, contact.Notes, contact.TrustLevel, []);
    }

    public async Task<ErrorOr<ContactMethodView>> AddContactMethodAsync(Guid contactId, string methodType, string value, string? displayValue, bool isPreferred, string? verificationStatus, string? notes, CancellationToken cancellationToken)
    {
        var contact = await dbContext.Set<ContactPerson>().SingleOrDefaultAsync(x => x.Id == contactId && x.IsActive, cancellationToken);
        if (contact is null)
        {
            return Error.NotFound("Customer.ContactNotFound", "Contact does not exist.");
        }

        var normalizedMethodType = NormalizeMethodType(methodType);
        if (normalizedMethodType.IsError)
        {
            return normalizedMethodType.Errors;
        }

        var normalizedValue = NormalizeContactValue(normalizedMethodType.Value, value);
        var effectiveDisplayValue = string.IsNullOrWhiteSpace(displayValue) ? value.Trim() : displayValue.Trim();
        var effectiveVerificationStatus = string.IsNullOrWhiteSpace(verificationStatus) ? ContactVerificationStatuses.Unverified : verificationStatus.Trim();

        var duplicate = await dbContext.Set<ContactMethod>()
            .AnyAsync(x => x.ContactPersonId == contactId && x.MethodType == normalizedMethodType.Value && x.NormalizedValue == normalizedValue, cancellationToken);
        if (duplicate)
        {
            return Error.Conflict("Customer.ContactMethodDuplicate", "A contact method with the same normalized value already exists for this contact.");
        }

        if (isPreferred)
        {
            var existingPreferred = await dbContext.Set<ContactMethod>()
                .Where(x => x.ContactPersonId == contactId && x.IsPreferred)
                .ToListAsync(cancellationToken);

            foreach (var method in existingPreferred)
            {
                method.IsPreferred = false;
                method.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        var utcNow = DateTime.UtcNow;
        var methodEntity = new ContactMethod
        {
            Id = Guid.NewGuid(),
            ContactPersonId = contactId,
            MethodType = normalizedMethodType.Value,
            NormalizedValue = normalizedValue,
            DisplayValue = effectiveDisplayValue,
            IsPreferred = isPreferred,
            VerificationStatus = effectiveVerificationStatus,
            IsActive = true,
            Notes = NormalizeOptional(notes),
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };

        dbContext.Set<ContactMethod>().Add(methodEntity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ContactMethodView(methodEntity.Id, methodEntity.MethodType, methodEntity.DisplayValue, methodEntity.IsPreferred, methodEntity.VerificationStatus, methodEntity.Notes);
    }

    public async Task<PetContactLinkView?> LinkContactToPetAsync(Guid petId, Guid contactId, IReadOnlyCollection<string> roleCodes, bool isPrimary, bool canPickUp, bool canPay, bool receivesNotifications, CancellationToken cancellationToken)
    {
        var contact = await dbContext.Set<ContactPerson>().SingleOrDefaultAsync(x => x.Id == contactId && x.IsActive, cancellationToken);
        if (contact is null)
        {
            return null;
        }

        var petExists = await petReferenceValidationService.ExistsAsync(petId, cancellationToken);
        if (!petExists)
        {
            return null;
        }

        var normalizedRoleCodes = NormalizeRoleCodes(roleCodes);
        var utcNow = DateTime.UtcNow;
        var existing = await dbContext.Set<PetContactLink>()
            .SingleOrDefaultAsync(x => x.PetId == petId && x.ContactPersonId == contactId, cancellationToken);

        if (existing is null)
        {
            existing = new PetContactLink
            {
                Id = Guid.NewGuid(),
                PetId = petId,
                ContactPersonId = contactId,
                CreatedAtUtc = utcNow
            };

            dbContext.Set<PetContactLink>().Add(existing);
        }

        existing.RoleCodes = string.Join(',', normalizedRoleCodes);
        existing.IsPrimary = isPrimary;
        existing.CanPickUp = canPickUp;
        existing.CanPay = canPay;
        existing.ReceivesNotifications = receivesNotifications;
        existing.UpdatedAtUtc = utcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        var methods = await dbContext.Set<ContactMethod>()
            .Where(x => x.ContactPersonId == contactId && x.IsActive)
            .OrderByDescending(x => x.IsPreferred)
            .ThenBy(x => x.MethodType)
            .Select(x => new ContactMethodView(x.Id, x.MethodType, x.DisplayValue, x.IsPreferred, x.VerificationStatus, x.Notes))
            .ToListAsync(cancellationToken);

        return new PetContactLinkView(
            petId,
            contact.Id,
            contact.ClientId,
            ComposeFullName(contact.FirstName, contact.LastName),
            normalizedRoleCodes,
            existing.IsPrimary,
            existing.CanPickUp,
            existing.CanPay,
            existing.ReceivesNotifications,
            methods);
    }

    public async Task<IReadOnlyCollection<PetContactLinkView>?> ListPetContactLinksAsync(Guid petId, Guid? actorUserId, CancellationToken cancellationToken)
    {
        var petExists = await petReferenceValidationService.ExistsAsync(petId, cancellationToken);
        if (!petExists)
        {
            return null;
        }

        var contacts = await dbContext.Set<PetContactLink>()
            .Where(x => x.PetId == petId)
            .Join(dbContext.Set<ContactPerson>(), x => x.ContactPersonId, y => y.Id, (x, y) => new { Link = x, Person = y })
            .OrderBy(x => x.Person.FirstName)
            .ThenBy(x => x.Person.LastName)
            .ToListAsync(cancellationToken);

        var contactIds = contacts.Select(x => x.Person.Id).ToArray();
        var methods = await dbContext.Set<ContactMethod>()
            .Where(x => contactIds.Contains(x.ContactPersonId) && x.IsActive)
            .OrderByDescending(x => x.IsPreferred)
            .ThenBy(x => x.MethodType)
            .ToListAsync(cancellationToken);

        if (actorUserId.HasValue)
        {
            await accessAuditService.RecordAsync("pet_contact_links", petId.ToString("D"), "READ_CONTACT_DATA", actorUserId, cancellationToken);
        }

        return contacts.Select(x => new PetContactLinkView(
                x.Link.PetId,
                x.Person.Id,
                x.Person.ClientId,
                ComposeFullName(x.Person.FirstName, x.Person.LastName),
                SplitRoleCodes(x.Link.RoleCodes),
                x.Link.IsPrimary,
                x.Link.CanPickUp,
                x.Link.CanPay,
                x.Link.ReceivesNotifications,
                methods.Where(m => m.ContactPersonId == x.Person.Id)
                    .Select(m => new ContactMethodView(m.Id, m.MethodType, m.DisplayValue, m.IsPreferred, m.VerificationStatus, m.Notes))
                    .ToArray()))
            .ToArray();
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

    private static IReadOnlyCollection<string> NormalizeRoleCodes(IReadOnlyCollection<string> roleCodes)
    {
        var normalized = roleCodes
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToArray();

        return normalized.Length == 0 ? [ContactRoleCodes.Owner] : normalized;
    }

    private static string[] SplitRoleCodes(string roleCodes)
    {
        return roleCodes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static string ComposeFullName(string firstName, string? lastName)
    {
        return string.Join(' ', new[] { firstName, lastName }.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
