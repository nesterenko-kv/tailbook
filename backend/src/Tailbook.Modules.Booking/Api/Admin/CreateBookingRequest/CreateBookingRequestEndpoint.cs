using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.Booking.Application;

namespace Tailbook.Modules.Booking.Api.Admin.CreateBookingRequest;

public sealed class CreateBookingRequestEndpoint(BookingManagementQueries bookingQueries)
    : Endpoint<CreateBookingRequestRequest, BookingRequestDetailView>
{
    public override void Configure()
    {
        Post("/api/admin/booking-requests");
        Description(x => x.WithTags("Admin Booking"));
        PermissionsAll("booking.write");
    }

    public override async Task HandleAsync(CreateBookingRequestRequest req, CancellationToken ct)
    {
        try
        {
            var result = await bookingQueries.CreateBookingRequestAsync(
                new CreateBookingRequestCommand(
                    req.ClientId,
                    req.PetId,
                    req.RequestedByContactId,
                    req.Channel,
                    req.Notes,
                    req.PreferredTimes.Select(x => new PreferredTimeWindowCommand(x.StartAtUtc, x.EndAtUtc, x.Label)).ToArray(),
                    req.Items.Select(x => new CreateBookingRequestItemCommand(x.OfferId, x.ItemType, x.RequestedNotes)).ToArray()),
                req.ActorUserId?.ToString("D"),
                ct);

            await Send.ResponseAsync(result, StatusCodes.Status201Created, ct);
        }
        catch (InvalidOperationException ex)
        {
            AddError(ex.Message);
            await Send.ErrorsAsync(cancellation: ct);
        }
    }
}

public sealed class CreateBookingRequestRequest
{
    [FromClaim(TailbookClaimTypes.UserId)]
    public Guid? ActorUserId { get; set; }

    public Guid? ClientId { get; set; }
    public Guid PetId { get; set; }
    public Guid? RequestedByContactId { get; set; }
    public string? Channel { get; set; }
    public string? Notes { get; set; }
    public PreferredTimeWindowPayload[] PreferredTimes { get; set; } = [];
    public BookingRequestItemPayload[] Items { get; set; } = [];
}

public sealed class PreferredTimeWindowPayload
{
    public DateTime StartAtUtc { get; set; }
    public DateTime EndAtUtc { get; set; }
    public string? Label { get; set; }
}

public sealed class BookingRequestItemPayload
{
    public Guid OfferId { get; set; }
    public string? ItemType { get; set; }
    public string? RequestedNotes { get; set; }
}

public sealed class CreateBookingRequestRequestValidator : Validator<CreateBookingRequestRequest>
{
    public CreateBookingRequestRequestValidator()
    {
        RuleFor(x => x.PetId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.OfferId).NotEmpty();
            item.RuleFor(x => x.ItemType).MaximumLength(32);
            item.RuleFor(x => x.RequestedNotes).MaximumLength(1000);
        });
        RuleForEach(x => x.PreferredTimes).ChildRules(time =>
        {
            time.RuleFor(x => x.StartAtUtc).NotEmpty();
            time.RuleFor(x => x.EndAtUtc).NotEmpty().GreaterThan(x => x.StartAtUtc);
            time.RuleFor(x => x.Label).MaximumLength(200);
        });
        RuleFor(x => x.Channel).MaximumLength(32);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}
