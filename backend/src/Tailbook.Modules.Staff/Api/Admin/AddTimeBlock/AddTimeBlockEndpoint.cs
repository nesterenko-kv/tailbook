using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.Staff.Application;

namespace Tailbook.Modules.Staff.Api.Admin.AddTimeBlock;

public sealed class AddTimeBlockEndpoint(ICurrentUser currentUser, IStaffAccessPolicy accessPolicy, StaffQueries staffQueries)
    : Endpoint<AddTimeBlockRequest, AddTimeBlockResponse>
{
    public override void Configure()
    {
        Post("/api/admin/groomers/{GroomerId:guid}/time-blocks");
        Description(x => x.WithTags("Admin Staff"));
    }

    public override async Task HandleAsync(AddTimeBlockRequest req, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        if (!accessPolicy.CanWriteStaff(currentUser))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        try
        {
            var block = await staffQueries.AddTimeBlockAsync(req.GroomerId, req.StartAtUtc, req.EndAtUtc, req.ReasonCode, req.Notes, ct);
            if (block is null)
            {
                await Send.NotFoundAsync(ct);
                return;
            }

            await Send.ResponseAsync(new AddTimeBlockResponse
            {
                Id = block.Id,
                GroomerId = block.GroomerId,
                StartAtUtc = block.StartAtUtc,
                EndAtUtc = block.EndAtUtc,
                ReasonCode = block.ReasonCode,
                Notes = block.Notes,
                CreatedAtUtc = block.CreatedAtUtc
            }, StatusCodes.Status201Created, ct);
        }
        catch (InvalidOperationException exception)
        {
            AddError(exception.Message);
            await Send.ErrorsAsync(cancellation: ct);
        }
    }
}

public sealed class AddTimeBlockRequest
{
    public Guid GroomerId { get; set; }
    public DateTime StartAtUtc { get; set; }
    public DateTime EndAtUtc { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public sealed class AddTimeBlockRequestValidator : Validator<AddTimeBlockRequest>
{
    public AddTimeBlockRequestValidator()
    {
        RuleFor(x => x.GroomerId).NotEmpty();
        RuleFor(x => x.ReasonCode).NotEmpty().MaximumLength(64);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public sealed class AddTimeBlockResponse
{
    public Guid Id { get; set; }
    public Guid GroomerId { get; set; }
    public DateTime StartAtUtc { get; set; }
    public DateTime EndAtUtc { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
