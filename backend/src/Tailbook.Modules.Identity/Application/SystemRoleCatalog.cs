using Tailbook.Modules.Identity.Contracts;

namespace Tailbook.Modules.Identity.Application;

public static class SystemRoleCatalog
{
    public static readonly IReadOnlyCollection<SystemPermissionDefinition> Permissions =
    [
        new(PermissionCodes.IamUsersRead, "Read IAM users"),
        new(PermissionCodes.IamUsersWrite, "Create and edit IAM users"),
        new(PermissionCodes.IamRolesRead, "Read roles and permissions"),
        new(PermissionCodes.IamRolesAssign, "Assign roles to users"),
        new(PermissionCodes.AuditAccessRead, "Read access audit entries"),
        new(PermissionCodes.AuditTrailRead, "Read audit trail entries"),
        new(PermissionCodes.ReportsRead, "Read reporting endpoints"),
        new(PermissionCodes.NotificationsRead, "Read notification jobs"),
        new(PermissionCodes.NotificationsWrite, "Process notification outbox and manage notifications"),
        new(PermissionCodes.CrmClientsRead, "Read CRM clients"),
        new(PermissionCodes.CrmClientsWrite, "Create and edit CRM clients"),
        new(PermissionCodes.CrmContactsRead, "Read CRM contact data"),
        new(PermissionCodes.CrmContactsWrite, "Create and edit CRM contact data"),
        new(PermissionCodes.PetsRead, "Read pets"),
        new(PermissionCodes.PetsWrite, "Create and edit pets"),
        new(PermissionCodes.PetsCatalogRead, "Read pet catalogs"),
        new(PermissionCodes.CatalogRead, "Read catalog offers and procedures"),
        new(PermissionCodes.CatalogWrite, "Create and edit catalog offers and procedures"),
        new(PermissionCodes.StaffRead, "Read groomers, schedules and availability"),
        new(PermissionCodes.StaffWrite, "Create and edit groomers, schedules and capabilities"),
        new(PermissionCodes.BookingRead, "Read booking requests, appointments and quote previews"),
        new(PermissionCodes.BookingWrite, "Create and edit booking requests and appointments"),
        new(PermissionCodes.VisitRead, "Read visits and execution details"),
        new(PermissionCodes.VisitWrite, "Check in, execute and finalize visits"),
        new(PermissionCodes.VisitAdjustmentsWrite, "Apply visit financial adjustments"),
        new(PermissionCodes.GroomerAppointmentsRead, "Read groomer-safe assigned appointments"),
        new(PermissionCodes.GroomerVisitsRead, "Read groomer-safe assigned visits"),
        new(PermissionCodes.GroomerVisitsWrite, "Execute groomer-safe visit actions"),
        new(PermissionCodes.AdminAppAccess, "Access admin application"),
        new(PermissionCodes.GroomerAppAccess, "Access groomer application"),
        new(PermissionCodes.ClientPortalAccess, "Access client portal"),
        new(PermissionCodes.ClientPetsRead, "Read own pets in client portal"),
        new(PermissionCodes.ClientAppointmentsRead, "Read own appointments in client portal"),
        new(PermissionCodes.ClientBookingWrite, "Submit booking requests in client portal"),
        new(PermissionCodes.ClientContactPreferencesRead, "Read own contact preferences in client portal"),
        new(PermissionCodes.ClientContactPreferencesWrite, "Update own contact preferences in client portal")
    ];

    public static readonly IReadOnlyCollection<SystemRoleDefinition> Roles =
    [
        new(RoleCodes.Admin, "Administrator", Permissions.Select(x => x.Code).ToArray()),
        new(RoleCodes.Manager, "Manager",
        [
            PermissionCodes.AdminAppAccess,
            PermissionCodes.CrmClientsRead,
            PermissionCodes.CrmClientsWrite,
            PermissionCodes.CrmContactsRead,
            PermissionCodes.CrmContactsWrite,
            PermissionCodes.PetsRead,
            PermissionCodes.PetsWrite,
            PermissionCodes.PetsCatalogRead,
            PermissionCodes.CatalogRead,
            PermissionCodes.CatalogWrite,
            PermissionCodes.StaffRead,
            PermissionCodes.StaffWrite,
            PermissionCodes.BookingRead,
            PermissionCodes.BookingWrite,
            PermissionCodes.VisitRead,
            PermissionCodes.VisitWrite,
            PermissionCodes.VisitAdjustmentsWrite,
            PermissionCodes.AuditTrailRead,
            PermissionCodes.ReportsRead,
            PermissionCodes.NotificationsRead,
            PermissionCodes.NotificationsWrite
        ]),
        new(RoleCodes.Groomer, "Groomer",
        [
            PermissionCodes.GroomerAppAccess,
            PermissionCodes.GroomerAppointmentsRead,
            PermissionCodes.GroomerVisitsRead,
            PermissionCodes.GroomerVisitsWrite
        ]),
        new(RoleCodes.Client, "Client",
        [
            PermissionCodes.ClientPortalAccess,
            PermissionCodes.ClientPetsRead,
            PermissionCodes.ClientAppointmentsRead,
            PermissionCodes.ClientBookingWrite,
            PermissionCodes.ClientContactPreferencesRead,
            PermissionCodes.ClientContactPreferencesWrite
        ])
    ];
}

public sealed record SystemPermissionDefinition(string Code, string DisplayName);
public sealed record SystemRoleDefinition(string Code, string DisplayName, IReadOnlyCollection<string> PermissionCodes);
