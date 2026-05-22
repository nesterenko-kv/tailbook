using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.BuildingBlocks.Infrastructure.Search;

namespace Tailbook.Modules.Customer.Infrastructure.Services;

public sealed class CustomerUseCases(
    AppDbContext dbContext,
    IPetReadModelService petReadModelService,
    IPetReferenceValidationService petReferenceValidationService,
    IAccessAuditService accessAuditService,
    TimeProvider timeProvider) : ICustomerReadService
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
        foreach (var term in SearchText.Terms(search))
        {
            if (dbContext.Database.IsNpgsql())
            {
                var pattern = SearchText.LikePattern(term);
                query = query.Where(client =>
                    EF.Functions.ILike(client.DisplayName, pattern, @"\") ||
                    (client.Notes != null && EF.Functions.ILike(client.Notes, pattern, @"\")) ||
                    dbContext.Set<ContactPerson>().Any(contact =>
                        contact.ClientId == client.Id &&
                        contact.IsActive &&
                        (EF.Functions.ILike(contact.FirstName, pattern, @"\") ||
                         (contact.LastName != null && EF.Functions.ILike(contact.LastName, pattern, @"\")) ||
                         EF.Functions.ILike(contact.FirstName + " " + (contact.LastName ?? string.Empty), pattern, @"\") ||
                         (contact.Notes != null && EF.Functions.ILike(contact.Notes, pattern, @"\")) ||
                         dbContext.Set<ContactMethod>().Any(method =>
                             method.ContactPersonId == contact.Id &&
                             method.IsActive &&
                             (EF.Functions.ILike(method.DisplayValue, pattern, @"\") ||
                              EF.Functions.ILike(method.NormalizedValue, pattern, @"\"))))));
            }
            else
            {
                query = query.Where(client =>
                    client.DisplayName.ToLower().Contains(term) ||
                    (client.Notes != null && client.Notes.ToLower().Contains(term)) ||
                    dbContext.Set<ContactPerson>().Any(contact =>
                        contact.ClientId == client.Id &&
                        contact.IsActive &&
                        (contact.FirstName.ToLower().Contains(term) ||
                         (contact.LastName != null && contact.LastName.ToLower().Contains(term)) ||
                         (contact.FirstName + " " + (contact.LastName ?? string.Empty)).ToLower().Contains(term) ||
                         (contact.Notes != null && contact.Notes.ToLower().Contains(term)) ||
                         dbContext.Set<ContactMethod>().Any(method =>
                             method.ContactPersonId == contact.Id &&
                             method.IsActive &&
                             (method.DisplayValue.ToLower().Contains(term) ||
                              method.NormalizedValue.ToLower().Contains(term))))));
            }
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
            x.CreatedAt,
            x.UpdatedAt)).ToArray();

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
            client.CreatedAt,
            client.UpdatedAt);
    }

    public async Task<ClientDetailView> CreateClientAsync(string displayName, string? notes, CancellationToken cancellationToken)
    {
        var utcNow = timeProvider.GetUtcNow();
        var client = Client.Create(displayName, notes, utcNow);

        dbContext.Set<Client>().Add(client);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ClientDetailView(client.Id, client.DisplayName, client.Status, client.Notes, [], [], client.CreatedAt, client.UpdatedAt);
    }

    public async Task<ContactPersonView?> AddContactPersonAsync(Guid clientId, string firstName, string? lastName, string? notes, string? trustLevel, CancellationToken cancellationToken)
    {
        var client = await dbContext.Set<Client>().SingleOrDefaultAsync(x => x.Id == clientId, cancellationToken);
        if (client is null)
        {
            return null;
        }

        var utcNow = timeProvider.GetUtcNow();
        var effectiveTrustLevel = string.IsNullOrWhiteSpace(trustLevel) ? ContactTrustLevels.Standard : trustLevel.Trim();
        var contact = client.AddContactPerson(firstName, lastName, notes, effectiveTrustLevel, true, utcNow);

        await dbContext.SaveChangesAsync(cancellationToken);

        return new ContactPersonView(contact.Id, contact.ClientId, contact.FirstName, contact.LastName, contact.Notes, contact.TrustLevel, []);
    }

    public async Task<ErrorOr<ContactMethodView>> AddContactMethodAsync(Guid contactId, string methodType, string value, string? displayValue, bool isPreferred, string? verificationStatus, string? notes, CancellationToken cancellationToken)
    {
        var contact = await dbContext.Set<ContactPerson>()
            .Include(x => x.Methods)
            .SingleOrDefaultAsync(x => x.Id == contactId && x.IsActive, cancellationToken);
        if (contact is null)
        {
            return Error.NotFound("Customer.ContactNotFound", "Contact does not exist.");
        }

        var normalizedMethodType = NormalizeMethodType(methodType);
        if (normalizedMethodType.IsError)
        {
            return normalizedMethodType.Errors;
        }

        var effectiveDisplayValue = string.IsNullOrWhiteSpace(displayValue) ? value.Trim() : displayValue.Trim();
        var effectiveVerificationStatus = string.IsNullOrWhiteSpace(verificationStatus) ? ContactVerificationStatuses.Unverified : verificationStatus.Trim();

        if (contact.GetMethod(normalizedMethodType.Value, value) is not null)
        {
            return Error.Conflict("Customer.ContactMethodDuplicate", "A contact method with the same normalized value already exists for this contact.");
        }

        var utcNow = timeProvider.GetUtcNow();

        if (isPreferred)
        {
            contact.ClearPreferredMethods(utcNow);
        }

        var methodEntity = contact.AddContactMethod(
            normalizedMethodType.Value,
            value,
            effectiveDisplayValue,
            isPreferred,
            effectiveVerificationStatus,
            utcNow);

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
        var utcNow = timeProvider.GetUtcNow();
        var existing = await dbContext.Set<PetContactLink>()
            .SingleOrDefaultAsync(x => x.PetId == petId && x.ContactPersonId == contactId, cancellationToken);

        if (existing is null)
        {
            existing = new PetContactLink
            {
                Id = Guid.NewGuid(),
                PetId = petId,
                ContactPersonId = contactId,
                CreatedAt = utcNow
            };

            dbContext.Set<PetContactLink>().Add(existing);
        }

        existing.RoleCodes = string.Join(',', normalizedRoleCodes);
        existing.IsPrimary = isPrimary;
        existing.CanPickUp = canPickUp;
        existing.CanPay = canPay;
        existing.ReceivesNotifications = receivesNotifications;
        existing.UpdatedAt = utcNow;

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

}
