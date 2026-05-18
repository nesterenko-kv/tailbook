using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.Modules.Staff.Domain.Events;

namespace Tailbook.Modules.Staff.Domain.Aggregates;

public sealed class Groomer : AggregateRoot
{
    private Groomer()
    {
    }

    public Guid? UserId { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public bool Active { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static Groomer Create(string displayName, Guid? userId, DateTimeOffset utcNow)
    {
        var entity = new Groomer
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DisplayName = displayName.Trim(),
            Active = true,
            CreatedAt = utcNow.ToUniversalTime(),
            UpdatedAt = utcNow.ToUniversalTime()
        };

        entity.RaiseDomainEvent(new GroomerCreatedDomainEvent(
            Guid.NewGuid(),
            utcNow.ToUniversalTime(),
            entity.Id,
            entity.UserId,
            entity.DisplayName,
            entity.Active));

        return entity;
    }
}
