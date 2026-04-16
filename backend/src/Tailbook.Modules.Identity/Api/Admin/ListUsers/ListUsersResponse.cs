namespace Tailbook.Modules.Identity.Api.Admin.ListUsers;

public sealed class ListUsersResponse
{
    public IReadOnlyCollection<UserItemResponse> Items { get; set; } = [];
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}

public sealed class UserItemResponse
{
    public Guid Id { get; set; }
    public string SubjectId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public IReadOnlyCollection<string> Roles { get; set; } = [];
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
