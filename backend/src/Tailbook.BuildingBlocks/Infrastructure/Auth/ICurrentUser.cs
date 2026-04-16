namespace Tailbook.BuildingBlocks.Infrastructure.Auth;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    string? UserId { get; }
    string? SubjectId { get; }
    IReadOnlyCollection<string> Roles { get; }
    IReadOnlyCollection<string> Permissions { get; }
}
