using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.Reporting.Application;

namespace Tailbook.Modules.Reporting.Api.Admin.GetEstimateAccuracy;

public sealed class GetEstimateAccuracyEndpoint(ICurrentUser currentUser, IReportingAccessPolicy accessPolicy, ReportingQueries reportingQueries)
    : Endpoint<GetEstimateAccuracyRequest, GetEstimateAccuracyResponse>
{
    public override void Configure()
    {
        Get("/api/admin/reports/estimate-accuracy");
        Description(x => x.WithTags("Reporting"));
    }

    public override async Task HandleAsync(GetEstimateAccuracyRequest req, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }
        if (!accessPolicy.CanReadReports(currentUser))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        var items = await reportingQueries.GetEstimateAccuracyAsync(req.FromUtc, req.ToUtc, ct);
        await Send.OkAsync(new GetEstimateAccuracyResponse { Items = items }, ct);
    }
}

public sealed class GetEstimateAccuracyRequest
{
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
}

public sealed class GetEstimateAccuracyResponse
{
    public IReadOnlyCollection<EstimateAccuracyReportItemView> Items { get; set; } = [];
}
