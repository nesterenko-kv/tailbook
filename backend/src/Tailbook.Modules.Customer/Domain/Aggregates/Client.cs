using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.Modules.Customer.Domain.Events;

namespace Tailbook.Modules.Customer.Domain.Aggregates;

public sealed class Client : AggregateRoot
{
    private Client()
    {
    }

    public string DisplayName { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

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
}
