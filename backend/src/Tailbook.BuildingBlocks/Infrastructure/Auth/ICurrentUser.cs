namespace Tailbook.BuildingBlocks.Infrastructure.Auth;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    string? SubjectId { get; }
    IReadOnlyCollection<string> Roles { get; }
}
