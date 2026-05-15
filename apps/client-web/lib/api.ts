import { ApiError, createApiError, createApiRequest, createImplicitAccessTokenPolicy, createPublicApiRequest, getApiErrorMessage } from "@tailbook/frontend-api";
import { resolveApiBaseUrl } from "./env";
import { createClientBrowserSessionRequest, getStoredAccessToken, getStoredRefreshToken, notifyUnauthorized, storeSession } from "./auth";

export { ApiError };

type RefreshResponse = {
    accessToken: string;
    refreshToken?: string | null;
    user: {
        email: string;
    };
};

const implicitAccessTokenPolicy = createImplicitAccessTokenPolicy({
    resolveApiBaseUrl,
    pathsWithoutImplicitAccessToken: [
        "/api/client/auth/login",
        "/api/client/auth/register",
        "/api/client/auth/refresh",
        "/api/client/auth/revoke",
        "/api/identity/auth/request-password-reset",
        "/api/identity/auth/reset-password"
    ]
});

async function refreshSession() {
    const refreshToken = getStoredRefreshToken();

    const response = await fetch(`${resolveApiBaseUrl()}/api/client/auth/refresh`, {
        ...createClientBrowserSessionRequest({
            method: "POST",
            headers: {
                "Accept": "application/json",
                "Content-Type": "application/json"
            },
            body: JSON.stringify(refreshToken ? { refreshToken } : {})
        }),
        cache: "no-store"
    }).catch(() => null);

    if (!response?.ok) {
        notifyUnauthorized();
        return false;
    }

    const payload = await response.json().catch(() => null) as RefreshResponse | null;
    if (!payload?.accessToken || !payload.user?.email) {
        notifyUnauthorized();
        return false;
    }

    storeSession(payload.accessToken, payload.user.email, payload.refreshToken);
    return true;
}

export const apiRequest = createApiRequest<ApiError>({
    resolveApiBaseUrl,
    getAccessToken: getStoredAccessToken,
    refreshSession,
    notifyUnauthorized,
    ...implicitAccessTokenPolicy,
    getErrorMessage: getApiErrorMessage,
    createError: createApiError
});

export const publicApiRequest = createPublicApiRequest<ApiError>({
    resolveApiBaseUrl,
    getErrorMessage: getApiErrorMessage,
    createError: createApiError
});
