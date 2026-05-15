namespace Tailbook.Modules.Identity.Application.Identity.Models;

public sealed record AuthenticatedUserView(Guid Id, string SubjectId, string Email, string DisplayName, string Status, Guid? ClientId, Guid? ContactPersonId, IReadOnlyCollection<string> Roles, IReadOnlyCollection<string> Permissions);