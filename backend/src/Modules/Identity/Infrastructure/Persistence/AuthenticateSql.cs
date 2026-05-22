namespace Tailbook.Modules.Identity.Infrastructure.Persistence;

internal static class AuthenticateSql
{
    internal const string LoginQuery = @"
        SELECT
            u.""Id"",
            u.""SubjectId"",
            u.""Email"",
            u.""NormalizedEmail"",
            u.""DisplayName"",
            u.""PasswordHash"",
            u.""Status"",
            u.""ClientId"",
            u.""ContactPersonId"",
            u.""CreatedAt"",
            u.""UpdatedAt"",
            COALESCE(r.role_codes, ARRAY[]::text[]) AS role_codes,
            COALESCE(p.permission_codes, ARRAY[]::text[]) AS permission_codes,
            EXISTS(
                SELECT 1
                FROM iam.iam_mfa_factors mfa
                WHERE mfa.""UserId"" = u.""Id""
                  AND mfa.""FactorType"" = 'EmailOtp'
                  AND mfa.""Status"" = 'Enabled'
                LIMIT 1
            ) AS has_mfa
        FROM iam.iam_users u
        LEFT JOIN LATERAL (
            SELECT ARRAY_AGG(DISTINCT r.""Code"" ORDER BY r.""Code"") AS role_codes
            FROM iam.iam_role_assignments ra
            JOIN iam.iam_roles r ON r.""Id"" = ra.""RoleId""
            WHERE ra.""UserId"" = u.""Id""
        ) r ON true
        LEFT JOIN LATERAL (
            SELECT ARRAY_AGG(DISTINCT p.""Code"" ORDER BY p.""Code"") AS permission_codes
            FROM iam.iam_role_assignments ra
            JOIN iam.iam_role_permissions rp ON rp.""RoleId"" = ra.""RoleId""
            JOIN iam.iam_permissions p ON p.""Id"" = rp.""PermissionId""
            WHERE ra.""UserId"" = u.""Id""
        ) p ON true
        WHERE u.""NormalizedEmail"" = @normalizedEmail";
}
