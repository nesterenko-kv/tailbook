namespace Tailbook.Modules.VisitOperations.Contracts;

public static class VisitStatusCodes
{
    public const string Open = "Open";
    public const string InProgress = "InProgress";
    public const string AwaitingFinalization = "AwaitingFinalization";
    public const string Closed = "Closed";

    public static readonly IReadOnlyCollection<string> All =
    [
        Open,
        InProgress,
        AwaitingFinalization,
        Closed
    ];
}
