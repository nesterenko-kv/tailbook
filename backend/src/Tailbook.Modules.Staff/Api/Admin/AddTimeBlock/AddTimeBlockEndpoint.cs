using FastEndpoints;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Http;

namespace Tailbook.Modules.Staff.Api.Admin.AddTimeBlock;

public sealed class AddTimeBlockEndpoint(IStaffQueries staffQueries)
    : Endpoint<AddTimeBlockRequest, AddTimeBlockResponse>
{
    public override void Configure()
    {
        Post("/api/admin/groomers/{GroomerId:guid}/time-blocks");
        Description(x => x.WithTags("Admin Staff"));
        PermissionsAll("staff.write");
    }

    public override async Task HandleAsync(AddTimeBlockRequest req, CancellationToken ct)
    {
        var result = await staffQueries.AddTimeBlockAsync(req.GroomerId, req.StartAtUtc, req.EndAtUtc, req.ReasonCode, req.Notes, ct);
        if (result.IsError)
        {
            await Send.ResultAsync(result.Errors.ToHttpResult());
            return;
        }

        var block = result.Value;
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
