using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Abstractions;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.Booking.Application;
using Tailbook.Modules.Identity.Contracts;

namespace Tailbook.Modules.Booking.Api.Client;

public sealed class CreateMyBookingRequestEndpoint(ICurrentUser currentUser, IClientPortalActorService actorService, ClientPortalBookingQueries queries)
    : Endpoint<CreateMyBookingRequestRequest, BookingRequestDetailView>
{
    public override void Configure()
    {
        Post("/api/client/booking-requests");
        Description(x => x.WithTags("Client Portal Booking"));
    }

    public override async Task HandleAsync(CreateMyBookingRequestRequest req, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated || !currentUser.HasPermission(PermissionCodes.ClientBookingWrite) || !Guid.TryParse(currentUser.UserId, out var userId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var actor = await actorService.GetActorAsync(userId, ct);
        if (actor is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        try
        {
            var result = await queries.CreateMyBookingRequestAsync(
                actor,
                new CreateClientBookingRequestCommand(
                    req.PetId,
                    req.Notes,
                    req.PreferredTimes.Select(x => new PreferredTimeWindowCommand(x.StartAtUtc, x.EndAtUtc, x.Label)).ToArray(),
                    req.Items.Select(x => new CreateClientBookingRequestItemCommand(x.OfferId, x.ItemType, x.RequestedNotes)).ToArray()),
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

public sealed class ListMyAppointmentsEndpoint(ICurrentUser currentUser, IClientPortalActorService actorService, ClientPortalBookingQueries queries)
    : EndpointWithoutRequest<IReadOnlyCollection<ClientAppointmentSummaryView>>
{
    public override void Configure()
    {
        Get("/api/client/appointments");
        Description(x => x.WithTags("Client Portal Booking"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated || !currentUser.HasPermission(PermissionCodes.ClientAppointmentsRead) || !Guid.TryParse(currentUser.UserId, out var userId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var actor = await actorService.GetActorAsync(userId, ct);
        if (actor is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var fromUtc = Query<DateTime?>("fromUtc", false);
        var result = await queries.ListMyAppointmentsAsync(actor.ClientId, fromUtc, ct);
        await Send.OkAsync(result, cancellation: ct);
    }
}

public sealed class GetMyAppointmentEndpoint(ICurrentUser currentUser, IClientPortalActorService actorService, ClientPortalBookingQueries queries)
    : EndpointWithoutRequest<ClientAppointmentDetailView>
{
    public override void Configure()
    {
        Get("/api/client/appointments/{appointmentId:guid}");
        Description(x => x.WithTags("Client Portal Booking"));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated || !currentUser.HasPermission(PermissionCodes.ClientAppointmentsRead) || !Guid.TryParse(currentUser.UserId, out var userId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var actor = await actorService.GetActorAsync(userId, ct);
        if (actor is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var appointmentId = Route<Guid>("appointmentId");
        var result = await queries.GetMyAppointmentAsync(actor.ClientId, appointmentId, ct);
        if (result is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(result, cancellation: ct);
    }
}

public sealed class CreateMyBookingRequestRequest
{
    public Guid PetId { get; set; }
    public string? Notes { get; set; }
    public ClientPreferredTimeWindowPayload[] PreferredTimes { get; set; } = [];
    public ClientBookingRequestItemPayload[] Items { get; set; } = [];
}

public sealed class ClientPreferredTimeWindowPayload
{
    public DateTime StartAtUtc { get; set; }
    public DateTime EndAtUtc { get; set; }
    public string? Label { get; set; }
}

public sealed class ClientBookingRequestItemPayload
{
    public Guid OfferId { get; set; }
    public string? ItemType { get; set; }
    public string? RequestedNotes { get; set; }
}

public sealed class CreateMyBookingRequestRequestValidator : Validator<CreateMyBookingRequestRequest>
{
    public CreateMyBookingRequestRequestValidator()
    {
        RuleFor(x => x.PetId).NotEmpty();
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).SetValidator(new ClientBookingRequestItemPayloadValidator());
        RuleForEach(x => x.PreferredTimes).SetValidator(new ClientPreferredTimeWindowPayloadValidator());
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

public sealed class ClientPreferredTimeWindowPayloadValidator : AbstractValidator<ClientPreferredTimeWindowPayload>
{
    public ClientPreferredTimeWindowPayloadValidator()
    {
        RuleFor(x => x.StartAtUtc).NotEmpty();
        RuleFor(x => x.EndAtUtc).NotEmpty().GreaterThan(x => x.StartAtUtc);
        RuleFor(x => x.Label).MaximumLength(200);
    }
}

public sealed class ClientBookingRequestItemPayloadValidator : AbstractValidator<ClientBookingRequestItemPayload>
{
    public ClientBookingRequestItemPayloadValidator()
    {
        RuleFor(x => x.OfferId).NotEmpty();
        RuleFor(x => x.ItemType).MaximumLength(32);
        RuleFor(x => x.RequestedNotes).MaximumLength(1000);
    }
}
