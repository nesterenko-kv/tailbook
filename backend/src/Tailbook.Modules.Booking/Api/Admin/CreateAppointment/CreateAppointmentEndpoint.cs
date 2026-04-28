using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.Booking.Application;

namespace Tailbook.Modules.Booking.Api.Admin.CreateAppointment;

public sealed class CreateAppointmentEndpoint(ICurrentUser currentUser, IBookingAccessPolicy accessPolicy, BookingManagementQueries bookingQueries)
    : Endpoint<CreateAppointmentRequest, AppointmentDetailView>
{
    public override void Configure()
    {
        Post("/api/admin/appointments");
        Description(x => x.WithTags("Admin Booking"));
    }

    public override async Task HandleAsync(CreateAppointmentRequest req, CancellationToken ct)
    {
        if (!accessPolicy.CanWriteBooking(currentUser))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        try
        {
            var result = await bookingQueries.CreateAppointmentAsync(
                new CreateAppointmentCommand(
                    req.PetId,
                    req.GroomerId,
                    req.StartAtUtc,
                    req.Items.Select(x => new CreateAppointmentItemCommand(x.OfferId, x.ItemType)).ToArray()),
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

public sealed class CreateAppointmentRequest
{
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
        RuleForEach(x => x.Items).SetValidator(new CreateAppointmentItemPayloadValidator());
    }
}

public sealed class CreateAppointmentItemPayloadValidator : AbstractValidator<CreateAppointmentItemPayload>
{
    public CreateAppointmentItemPayloadValidator()
    {
        RuleFor(x => x.OfferId).NotEmpty();
        RuleFor(x => x.ItemType).MaximumLength(32);
    }
}
