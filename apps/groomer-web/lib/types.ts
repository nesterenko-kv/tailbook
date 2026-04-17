export type IdentityMeResponse = {
    userId?: string | null;
    subjectId: string;
    email: string;
    displayName: string;
    clientId?: string | null;
    contactPersonId?: string | null;
    roles: string[];
    permissions: string[];
};
