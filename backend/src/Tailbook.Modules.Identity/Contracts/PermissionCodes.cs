namespace Tailbook.Modules.Identity.Contracts;

public static class PermissionCodes
{
    public const string IamUsersRead = "iam.users.read";
    public const string IamUsersWrite = "iam.users.write";
    public const string IamRolesRead = "iam.roles.read";
    public const string IamRolesAssign = "iam.roles.assign";
    public const string AuditAccessRead = "audit.access.read";
    public const string CrmContactsRead = "crm.contacts.read";
    public const string AdminAppAccess = "app.admin.access";
    public const string GroomerAppAccess = "app.groomer.access";
    public const string ClientPortalAccess = "app.client.access";
}
