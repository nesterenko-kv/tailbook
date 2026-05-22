using FastEndpoints;
using Microsoft.AspNetCore.Http;

namespace Tailbook.Modules.Reporting.Api.Admin.GetPackagePerformance;

public sealed class GetPackagePerformanceEndpoint(IReportingReadService reportingReadService)
    : Endpoint<GetPackagePerformanceRequest, GetPackagePerformanceResponse>
{
    public override void Configure()
    {
        Get("/api/admin/reports/package-performance");
        Description(x => x.WithTags("Reporting"));
        PermissionsAll("reports.read");
    }

    public override async Task HandleAsync(GetPackagePerformanceRequest req, CancellationToken ct)
    {
        var items = await reportingReadService.GetPackagePerformanceAsync(req.From, req.To, ct);
        await Send.OkAsync(new GetPackagePerformanceResponse { Items = items }, ct);
    }
}