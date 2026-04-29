import { createApiRequest } from "@tailbook/frontend-api";
import { resolveApiBaseUrl } from "./env";
import { getStoredAccessToken, getStoredRefreshToken, notifyUnauthorized, storeSession } from "./auth";

export class ApiError extends Error {
    status: number;

    constructor(status: number, message: string) {
        super(message);
        this.status = status;
    }
}

type RefreshResponse = {
    accessToken: string;
    refreshToken: string;
    user: {
        email: string;
    };
};

function shouldAttemptRefresh(path: string, init?: RequestInit) {
    if (typeof window === "undefined") return false;
    if (new Headers(init?.headers ?? {}).has("Authorization")) return false;
    return !path.includes("/api/client/auth/login")
        && !path.includes("/api/client/auth/register")
        && !path.includes("/api/client/auth/refresh")
        && !path.includes("/api/client/auth/revoke");
}

async function refreshSession() {
    const refreshToken = getStoredRefreshToken();
    if (!refreshToken) return false;

    const response = await fetch(`${resolveApiBaseUrl()}/api/client/auth/refresh`, {
        method: "POST",
        headers: {
            "Accept": "application/json",
            "Content-Type": "application/json"
        },
        body: JSON.stringify({ refreshToken }),
        cache: "no-store"
    }).catch(() => null);

    if (!response?.ok) {
        notifyUnauthorized();
        return false;
    }

    const payload = await response.json().catch(() => null) as RefreshResponse | null;
    if (!payload?.accessToken || !payload.refreshToken || !payload.user?.email) {
        notifyUnauthorized();
        return false;
    }

    storeSession(payload.accessToken, payload.user.email, payload.refreshToken);
    return true;
}

function getErrorMessage(payload: unknown, response: Response) {
    if (payload && typeof payload === "object") {
        const body = payload as { message?: unknown; errors?: unknown; title?: unknown };
        if (typeof body.message === "string") return body.message;
        if (Array.isArray(body.errors) && body.errors.length > 0) return body.errors.join(" ");
        if (body.errors && typeof body.errors === "object" && "generalErrors" in body.errors) {
            const generalErrors = (body.errors as { generalErrors?: unknown }).generalErrors;
            if (Array.isArray(generalErrors) && generalErrors.length > 0) return generalErrors.join(" ");
        }
        if (typeof body.title === "string") return body.title;
    }

    if (typeof payload === "string" && payload.length > 0) return payload;
    return `Request failed with status ${response.status}`;
}

export const apiRequest = createApiRequest<ApiError>({
    resolveApiBaseUrl,
    getAccessToken: getStoredAccessToken,
    refreshSession,
    notifyUnauthorized,
    shouldAttemptRefresh,
    getErrorMessage,
    createError: (message, status) => new ApiError(status, message)
});
