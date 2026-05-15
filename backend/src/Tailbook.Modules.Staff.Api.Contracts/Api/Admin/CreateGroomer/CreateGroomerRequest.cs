namespace Tailbook.Modules.Staff.Api.Admin.CreateGroomer;

public sealed class CreateGroomerRequest
{
    public Guid? UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}