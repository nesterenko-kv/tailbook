using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.Reporting.Application;

namespace Tailbook.Modules.Reporting.Api.Admin.GetEstimateAccuracyReport;

public sealed class GetEstimateAccuracyReportEndpoint(
    ICurrentUser currentUser,
    ReportingQueries reportingQueries) : Endpoint<GetEstimateAccuracyReportRequest, EstimateAccuracyReportView>
{
    private const string ReportsReadPermission = "reports.read";

    public override void Configure()
    {
        Get("/api/admin/reports/estimate-accuracy");
        Description(x => x.WithTags("Reporting"));
    }

    public override async Task HandleAsync(GetEstimateAccuracyReportRequest req, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        if (!currentUser.HasPermission(ReportsReadPermission))
        {
            await Send.ForbiddenAsync(ct);
            return;
        }

        var items = await reportingQueries.GetEstimateAccuracyAsync(req.FromUtc, req.ToUtc, ct);
        await Send.OkAsync(new EstimateAccuracyReportView { Items = items }, ct);
    }
}

public sealed class GetEstimateAccuracyReportRequest
{
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
}

public sealed class EstimateAccuracyReportView
{
    public IReadOnlyCollection<EstimateAccuracyReportItemView> Items { get; set; } = [];
}
