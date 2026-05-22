using Microsoft.EntityFrameworkCore;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Persistence;

namespace Tailbook.Modules.Customer.Infrastructure.Services;

public sealed class CustomerReferenceServices(AppDbContext dbContext, TimeProvider timeProvider)
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

    public async Task<ClientOnboardingResult> CreateClientPortalProfileAsync(CreateClientPortalProfileInput command, CancellationToken cancellationToken)
    {
        var utcNow = timeProvider.GetUtcNow();
        var client = Client.Create(command.DisplayName, "Created from client portal registration.", utcNow);

        var contact = client.AddContactPerson(
            command.FirstName.Trim(),
            command.LastName,
            "Primary client portal contact.",
            ContactTrustLevels.Standard,
            true,
            utcNow);

        contact.AddContactMethod(
            ContactMethodTypes.Email,
            command.Email,
            command.Email.Trim(),
            true,
            ContactVerificationStatuses.Unverified,
            utcNow);

        if (!string.IsNullOrWhiteSpace(command.Phone))
        {
            contact.AddContactMethod(
                ContactMethodTypes.Phone,
                command.Phone!,
                command.Phone!.Trim(),
                false,
                ContactVerificationStatuses.Unverified,
                utcNow);
        }

        if (!string.IsNullOrWhiteSpace(command.Instagram))
        {
            contact.AddContactMethod(
                ContactMethodTypes.Instagram,
                command.Instagram!,
                command.Instagram!.Trim(),
                false,
                ContactVerificationStatuses.Unverified,
                utcNow);
        }

        dbContext.Set<Client>().Add(client);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ClientOnboardingResult(client.Id, contact.Id);
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
}
