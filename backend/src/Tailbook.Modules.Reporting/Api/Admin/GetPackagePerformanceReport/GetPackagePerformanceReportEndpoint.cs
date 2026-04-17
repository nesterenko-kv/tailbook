using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Tailbook.BuildingBlocks.Infrastructure.Auth;
using Tailbook.Modules.Reporting.Application;

namespace Tailbook.Modules.Reporting.Api.Admin.GetPackagePerformanceReport;

public sealed class GetPackagePerformanceReportEndpoint(
    ICurrentUser currentUser,
    ReportingQueries reportingQueries) : Endpoint<GetPackagePerformanceReportRequest, PackagePerformanceReportView>
{
    private const string ReportsReadPermission = "reports.read";

    public override void Configure()
    {
        Get("/api/admin/reports/package-performance");
        Description(x => x.WithTags("Reporting"));
    }

    public override async Task HandleAsync(GetPackagePerformanceReportRequest req, CancellationToken ct)
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

        var report = await reportingQueries.GetPackagePerformanceAsync(req.FromUtc, req.ToUtc, ct);
        await Send.OkAsync(report, ct);
    }
}
