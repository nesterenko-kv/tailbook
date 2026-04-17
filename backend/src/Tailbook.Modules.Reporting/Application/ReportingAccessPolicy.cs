using Tailbook.BuildingBlocks.Infrastructure.Auth;

namespace Tailbook.Modules.Reporting.Application;

public interface IReportingAccessPolicy
{
    bool CanReadReports(ICurrentUser currentUser);
}

public sealed class ReportingAccessPolicy : IReportingAccessPolicy
{
    private const string ReportsReadPermission = "reports.read";

    public bool CanReadReports(ICurrentUser currentUser)
        => currentUser.IsAuthenticated && currentUser.HasPermission(ReportsReadPermission);
}
