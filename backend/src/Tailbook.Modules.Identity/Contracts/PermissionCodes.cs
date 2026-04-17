namespace Tailbook.Modules.Identity.Contracts;

public static class PermissionCodes
{
    public const string IamUsersRead = "iam.users.read";
    public const string IamUsersWrite = "iam.users.write";
    public const string IamRolesRead = "iam.roles.read";
    public const string IamRolesAssign = "iam.roles.assign";
    public const string AuditAccessRead = "audit.access.read";
    public const string AuditTrailRead = "audit.trail.read";
    public const string ReportsRead = "reports.read";
    public const string NotificationsRead = "notifications.read";
    public const string NotificationsWrite = "notifications.write";
    public const string CrmClientsRead = "crm.clients.read";
    public const string CrmClientsWrite = "crm.clients.write";
    public const string CrmContactsRead = "crm.contacts.read";
    public const string CrmContactsWrite = "crm.contacts.write";
    public const string PetsRead = "pets.read";
    public const string PetsWrite = "pets.write";
    public const string PetsCatalogRead = "pets.catalog.read";
    public const string CatalogRead = "catalog.read";
    public const string CatalogWrite = "catalog.write";
    public const string StaffRead = "staff.read";
    public const string StaffWrite = "staff.write";
    public const string BookingRead = "booking.read";
    public const string BookingWrite = "booking.write";
    public const string VisitRead = "visit.read";
    public const string VisitWrite = "visit.write";
    public const string GroomerAppointmentsRead = "groomer.appointments.read";
    public const string GroomerVisitsRead = "groomer.visits.read";
    public const string GroomerVisitsWrite = "groomer.visits.write";
    public const string AdminAppAccess = "app.admin.access";
    public const string GroomerAppAccess = "app.groomer.access";
    public const string ClientPortalAccess = "app.client.access";
    public const string ClientPetsRead = "client.pets.read";
    public const string ClientAppointmentsRead = "client.appointments.read";
    public const string ClientBookingWrite = "client.booking.write";
    public const string ClientContactPreferencesRead = "client.contact_preferences.read";
    public const string ClientContactPreferencesWrite = "client.contact_preferences.write";
}
