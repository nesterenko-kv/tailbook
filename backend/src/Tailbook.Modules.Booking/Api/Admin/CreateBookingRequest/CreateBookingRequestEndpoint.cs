using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.Booking.Application;

namespace Tailbook.Modules.Booking.Api.Admin.CreateBookingRequest;

public sealed class CreateBookingRequestEndpoint(ICurrentUser currentUser, IBookingAccessPolicy accessPolicy, BookingManagementQueries bookingQueries)
    : Endpoint<CreateBookingRequestRequest, BookingRequestDetailView>
{
    public override void Configure()
    {
        Post("/api/admin/booking-requests");
        Description(x => x.WithTags("Admin Booking"));
    }

    public override async Task HandleAsync(CreateBookingRequestRequest req, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        if (!accessPolicy.CanWriteBooking(currentUser))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

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
                currentUser.UserId,
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
        RuleForEach(x => x.Items).SetValidator(new BookingRequestItemPayloadValidator());
        RuleForEach(x => x.PreferredTimes).SetValidator(new PreferredTimeWindowPayloadValidator());
        RuleFor(x => x.Channel).MaximumLength(32);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

public sealed class PreferredTimeWindowPayloadValidator : AbstractValidator<PreferredTimeWindowPayload>
{
    public PreferredTimeWindowPayloadValidator()
    {
        RuleFor(x => x.StartAtUtc).NotEmpty();
        RuleFor(x => x.EndAtUtc).NotEmpty().GreaterThan(x => x.StartAtUtc);
        RuleFor(x => x.Label).MaximumLength(200);
    }
}

public sealed class BookingRequestItemPayloadValidator : AbstractValidator<BookingRequestItemPayload>
{
    public BookingRequestItemPayloadValidator()
    {
        RuleFor(x => x.OfferId).NotEmpty();
        RuleFor(x => x.ItemType).MaximumLength(32);
        RuleFor(x => x.RequestedNotes).MaximumLength(1000);
    }
}
