namespace Tailbook.Modules.VisitOperations.Domain;

public static class VisitStatusCodes
{
    public const string Open = "Open";
    public const string InProgress = "InProgress";
    public const string AwaitingFinalization = "AwaitingFinalization";
    public const string Closed = "Closed";
    public const string Cancelled = "Cancelled";

    public static readonly IReadOnlyCollection<string> All =
    [
        Open,
        InProgress,
        AwaitingFinalization,
        Closed,
        Cancelled
    ];
}
