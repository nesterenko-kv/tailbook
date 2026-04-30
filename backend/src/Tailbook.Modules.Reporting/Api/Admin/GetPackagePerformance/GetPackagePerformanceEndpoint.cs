using FastEndpoints;
using Microsoft.AspNetCore.Http;

namespace Tailbook.Modules.Reporting.Api.Admin.GetPackagePerformance;

public sealed class GetPackagePerformanceEndpoint(ReportingQueries reportingQueries)
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
        var items = await reportingQueries.GetPackagePerformanceAsync(req.FromUtc, req.ToUtc, ct);
        await Send.OkAsync(new GetPackagePerformanceResponse { Items = items }, ct);
    }
}

public sealed class GetPackagePerformanceRequest
{
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
}

public sealed class GetPackagePerformanceResponse
{
    public IReadOnlyCollection<PackagePerformanceReportItemView> Items { get; set; } = [];
}
