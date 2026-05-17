using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.Modules.Booking.Domain.Events;

namespace Tailbook.Modules.Booking.Domain.Aggregates;

public sealed class BookingRequest : AggregateRoot
{
    private BookingRequest()
    {
    }

    public Guid? ClientId { get; private set; }
    public Guid? PetId { get; private set; }
    public Guid? RequestedByContactId { get; private set; }
    public Guid? PreferredGroomerId { get; private set; }
    public string Channel { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;
    public string? SelectionMode { get; private set; }
    public string? GuestIntakeJson { get; private set; }
    public string? PreferredTimeJson { get; private set; }
    public string? Notes { get; private set; }
    public int VersionNo { get; private set; } = 1;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static BookingRequest Create(
        Guid id,
        Guid? clientId,
        Guid? petId,
        Guid? requestedByContactId,
        Guid? preferredGroomerId,
        string channel,
        string status,
        string? selectionMode,
        string? guestIntakeJson,
        string? preferredTimeJson,
        string? notes,
        DateTimeOffset utcNow)
    {
        var createdAt = utcNow.ToUniversalTime();
        var entity = new BookingRequest
        {
            Id = id,
            ClientId = clientId,
            PetId = petId,
            RequestedByContactId = requestedByContactId,
            PreferredGroomerId = preferredGroomerId,
            Channel = channel,
            Status = status,
            SelectionMode = selectionMode,
            GuestIntakeJson = guestIntakeJson,
            PreferredTimeJson = preferredTimeJson,
            Notes = notes,
            VersionNo = 1,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };

        entity.RaiseDomainEvent(new BookingRequestedDomainEvent(
            Guid.NewGuid(),
            createdAt,
            entity.Id,
            entity.PetId,
            entity.ClientId,
            entity.Channel,
            entity.Status,
            entity.SelectionMode));

        return entity;
    }

    public void MarkConverted(Guid appointmentId, DateTimeOffset utcNow)
    {
        Status = BookingRequestStatusCodes.Converted;
        UpdatedAt = utcNow.ToUniversalTime();
        VersionNo += 1;

        RaiseDomainEvent(new BookingRequestConvertedDomainEvent(
            Guid.NewGuid(),
            UpdatedAt,
            Id,
            appointmentId));
    }

    public void AttachContext(Guid? clientId, Guid petId, Guid? requestedByContactId, DateTimeOffset utcNow)
    {
        ClientId = clientId;
        PetId = petId;
        RequestedByContactId = requestedByContactId;
        UpdatedAt = utcNow.ToUniversalTime();

        if (Status == BookingRequestStatusCodes.NeedsReview)
        {
            Status = BookingRequestStatusCodes.Submitted;
        }
    }
}
