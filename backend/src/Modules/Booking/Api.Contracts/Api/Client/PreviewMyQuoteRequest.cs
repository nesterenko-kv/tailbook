using FastEndpoints;
using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.Booking.Api.Client;

public sealed class PreviewMyQuoteRequest
{
    [FromClaim(TailbookClaimTypes.UserId)]
    public Guid UserId { get; set; }

    public Guid PetId { get; set; }
    public PreviewMyQuoteItemRequest[] Items { get; set; } = [];
}