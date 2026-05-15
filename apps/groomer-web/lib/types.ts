export type IdentityMeResponse = {
    userId: string;
    subjectId: string;
    email: string;
    displayName: string;
    clientId?: string | null;
    contactPersonId?: string | null;
    roles: string[];
    permissions: string[];
};

export type AuthenticatedUser = {
    id: string;
    email: string;
    displayName: string;
    roles: string[];
    permissions: string[];
};

export type AuthenticatedLoginResponse = {
    status: "Authenticated";
    accessToken: string;
    expiresAt: string;
    refreshToken?: string | null;
    refreshTokenExpiresAt: string;
    user: AuthenticatedUser;
    mfaChallenge?: null;
};

export type MfaChallenge = {
    challengeId: string;
    factorType: string;
    expiresAt: string;
};

export type MfaRequiredLoginResponse = {
    status: "MfaRequired";
    accessToken?: null;
    expiresAt?: null;
    refreshToken?: null;
    refreshTokenExpiresAt?: null;
    user?: null;
    mfaChallenge: MfaChallenge;
};

export type LoginResponse = AuthenticatedLoginResponse | MfaRequiredLoginResponse;
