using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.Modules.Customer.Domain.Entities;
using Tailbook.Modules.Customer.Domain.Events;

namespace Tailbook.Modules.Customer.Domain.Aggregates;

public sealed class Client : AggregateRoot
{
    private readonly List<ContactPerson> _contacts = [];

    private Client()
    {
    }

    public string DisplayName { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public IReadOnlyCollection<ContactPerson> Contacts => _contacts.AsReadOnly();

    public static Client Create(string displayName, string? notes, DateTimeOffset utcNow)
    {
        var client = new Client
        {
            Id = Guid.NewGuid(),
            DisplayName = displayName.Trim(),
            Status = ClientStatusCodes.Active,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            CreatedAt = utcNow.ToUniversalTime(),
            UpdatedAt = utcNow.ToUniversalTime()
        };

        client.RaiseDomainEvent(new ClientCreatedDomainEvent(
            Guid.NewGuid(),
            utcNow.ToUniversalTime(),
            client.Id,
            client.DisplayName,
            client.Status,
            client.Notes));

        return client;
    }

    public ContactPerson AddContactPerson(
        string firstName,
        string? lastName,
        string? notes,
        string trustLevel,
        bool isActive,
        DateTimeOffset utcNow)
    {
        var contact = ContactPerson.Create(
            Guid.NewGuid(),
            Id,
            firstName,
            lastName,
            notes,
            trustLevel,
            isActive,
            utcNow);

        _contacts.Add(contact);
        return contact;
    }
}
