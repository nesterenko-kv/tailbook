using FastEndpoints;
using Microsoft.AspNetCore.Http;

namespace Tailbook.Modules.Reporting.Api.Admin.GetEstimateAccuracy;

public sealed class GetEstimateAccuracyEndpoint(IReportingReadService reportingReadService)
    : Endpoint<GetEstimateAccuracyRequest, GetEstimateAccuracyResponse>
{
    public override void Configure()
    {
        Get("/api/admin/reports/estimate-accuracy");
        Description(x => x.WithTags("Reporting"));
        PermissionsAll("reports.read");
    }

    public override async Task HandleAsync(GetEstimateAccuracyRequest req, CancellationToken ct)
    {
        var items = await reportingReadService.GetEstimateAccuracyAsync(req.FromUtc, req.ToUtc, ct);
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
