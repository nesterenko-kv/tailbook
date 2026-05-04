using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;
using Tailbook.Modules.Customer.Contracts;

namespace Tailbook.Modules.Customer.Infrastructure.Services;

public sealed partial class CustomerReferenceServices(AppDbContext dbContext)
    : IClientReferenceValidationService,
      IContactReferenceValidationService,
      IPetContactReadModelService,
      IClientOnboardingService
{
    public async Task<bool> ExistsAsync(Guid clientId, CancellationToken cancellationToken)
    {
        return await dbContext.Set<Client>().AnyAsync(x => x.Id == clientId, cancellationToken);
    }

    async Task<bool> IContactReferenceValidationService.ExistsAsync(Guid contactId, CancellationToken cancellationToken)
    {
        return await dbContext.Set<ContactPerson>().AnyAsync(x => x.Id == contactId && x.IsActive, cancellationToken);
    }

    public async Task<IReadOnlyCollection<PetContactAdminSummary>> GetPetContactsAsync(Guid petId, CancellationToken cancellationToken)
    {
        var links = await dbContext.Set<PetContactLink>()
            .Where(x => x.PetId == petId)
            .Join(dbContext.Set<ContactPerson>(), x => x.ContactPersonId, y => y.Id, (x, y) => new { Link = x, Person = y })
            .OrderBy(x => x.Person.FirstName)
            .ThenBy(x => x.Person.LastName)
            .ToListAsync(cancellationToken);

        var contactIds = links.Select(x => x.Person.Id).Distinct().ToArray();
        var methods = await dbContext.Set<ContactMethod>()
            .Where(x => contactIds.Contains(x.ContactPersonId) && x.IsActive)
            .OrderByDescending(x => x.IsPreferred)
            .ThenBy(x => x.MethodType)
            .Select(x => new { x.ContactPersonId, Method = new ContactMethodAdminSummary(x.Id, x.MethodType, x.DisplayValue, x.IsPreferred, x.VerificationStatus) })
            .ToListAsync(cancellationToken);

        return links.Select(x => new PetContactAdminSummary(
            x.Person.Id,
            x.Person.ClientId,
            ComposeFullName(x.Person.FirstName, x.Person.LastName),
            x.Link.IsPrimary,
            x.Link.CanPickUp,
            x.Link.CanPay,
            x.Link.ReceivesNotifications,
            SplitRoleCodes(x.Link.RoleCodes),
            methods.Where(m => m.ContactPersonId == x.Person.Id).Select(m => m.Method).ToArray())).ToArray();
    }

    public async Task<ClientOnboardingResult> CreateClientPortalProfileAsync(CreateClientPortalProfileCommand command, CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;
        var client = new Client
        {
            Id = Guid.NewGuid(),
            DisplayName = command.DisplayName.Trim(),
            Status = ClientStatusCodes.Active,
            Notes = "Created from client portal registration.",
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };

        var contact = new ContactPerson
        {
            Id = Guid.NewGuid(),
            ClientId = client.Id,
            FirstName = command.FirstName.Trim(),
            LastName = NormalizeOptional(command.LastName),
            Notes = "Primary client portal contact.",
            TrustLevel = ContactTrustLevels.Standard,
            IsActive = true,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };

        dbContext.Set<Client>().Add(client);
        dbContext.Set<ContactPerson>().Add(contact);

        var methods = new List<ContactMethod>
        {
            CreateMethod(contact.Id, ContactMethodTypes.Email, command.Email, command.Email.Trim(), true, utcNow)
        };

        if (!string.IsNullOrWhiteSpace(command.Phone))
        {
            methods.Add(CreateMethod(contact.Id, ContactMethodTypes.Phone, command.Phone!, command.Phone!.Trim(), false, utcNow));
        }

        if (!string.IsNullOrWhiteSpace(command.Instagram))
        {
            methods.Add(CreateMethod(contact.Id, ContactMethodTypes.Instagram, command.Instagram!, command.Instagram!.Trim(), false, utcNow));
        }

        dbContext.Set<ContactMethod>().AddRange(methods);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ClientOnboardingResult(client.Id, contact.Id);
    }

    private static ContactMethod CreateMethod(Guid contactPersonId, string methodType, string rawValue, string displayValue, bool isPreferred, DateTime utcNow)
    {
        return new ContactMethod
        {
            Id = Guid.NewGuid(),
            ContactPersonId = contactPersonId,
            MethodType = methodType,
            NormalizedValue = NormalizeValue(methodType, rawValue),
            DisplayValue = displayValue,
            IsPreferred = isPreferred,
            VerificationStatus = ContactVerificationStatuses.Unverified,
            IsActive = true,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    private static string NormalizeValue(string methodType, string value)
    {
        var trimmed = value.Trim();
        return methodType switch
        {
            ContactMethodTypes.Phone => DigitsRegex().Replace(trimmed, string.Empty),
            ContactMethodTypes.Instagram => trimmed.TrimStart('@').ToLowerInvariant(),
            ContactMethodTypes.Email => trimmed.ToLowerInvariant(),
            _ => trimmed
        };
    }

    private static string ComposeFullName(string firstName, string? lastName)
    {
        return string.Join(' ', new[] { firstName, lastName }.Where(x => !string.IsNullOrWhiteSpace(x)));
    }

    private static string[] SplitRoleCodes(string roleCodes)
    {
        return roleCodes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    [GeneratedRegex("[^0-9+]")]
    private static partial Regex DigitsRegex();
}
