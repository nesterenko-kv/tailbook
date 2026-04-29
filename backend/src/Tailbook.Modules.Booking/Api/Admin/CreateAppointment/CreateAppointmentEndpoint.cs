using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.Booking.Application;

namespace Tailbook.Modules.Booking.Api.Admin.CreateAppointment;

public sealed class CreateAppointmentEndpoint(BookingManagementQueries bookingQueries)
    : Endpoint<CreateAppointmentRequest, AppointmentDetailView>
{
    public override void Configure()
    {
        Post("/api/admin/appointments");
        Description(x => x.WithTags("Admin Booking"));
        PermissionsAll("booking.write");
    }

    public override async Task HandleAsync(CreateAppointmentRequest req, CancellationToken ct)
    {
        try
        {
            var result = await bookingQueries.CreateAppointmentAsync(
                new CreateAppointmentCommand(
                    req.PetId,
                    req.GroomerId,
                    req.StartAtUtc,
                    req.Items.Select(x => new CreateAppointmentItemCommand(x.OfferId, x.ItemType)).ToArray()),
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

public sealed class CreateAppointmentRequest
{
    [FromClaim(TailbookClaimTypes.UserId)]
    public Guid? ActorUserId { get; set; }

    public Guid PetId { get; set; }
    public Guid GroomerId { get; set; }
    public DateTime StartAtUtc { get; set; }
    public CreateAppointmentItemPayload[] Items { get; set; } = [];
}

public sealed class CreateAppointmentItemPayload
{
    public Guid OfferId { get; set; }
    public string? ItemType { get; set; }
}

public sealed class CreateAppointmentRequestValidator : Validator<CreateAppointmentRequest>
{
    public CreateAppointmentRequestValidator()
    {
        RuleFor(x => x.PetId).NotEmpty();
        RuleFor(x => x.GroomerId).NotEmpty();
        RuleFor(x => x.StartAtUtc).NotEmpty();
        RuleFor(x => x.Items).NotEmpty();
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(x => x.OfferId).NotEmpty();
            item.RuleFor(x => x.ItemType).MaximumLength(32);
        });
    }
}
