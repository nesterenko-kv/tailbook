namespace Tailbook.Modules.Identity.Api.GetMe;

public sealed class GetMeResponse
{
    public bool IsAuthenticated { get; init; }
    public string? SubjectId { get; init; }
    public IReadOnlyCollection<string> Roles { get; init; } = Array.Empty<string>();
}
