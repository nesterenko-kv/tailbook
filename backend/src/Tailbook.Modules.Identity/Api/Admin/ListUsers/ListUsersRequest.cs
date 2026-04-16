namespace Tailbook.Modules.Identity.Api.Admin.ListUsers;

public sealed class ListUsersRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
