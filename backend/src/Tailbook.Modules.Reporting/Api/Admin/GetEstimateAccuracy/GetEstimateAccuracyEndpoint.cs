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
        var items = await reportingReadService.GetEstimateAccuracyAsync(req.From, req.To, ct);
        await Send.OkAsync(new GetEstimateAccuracyResponse { Items = items }, ct);
    }
}
