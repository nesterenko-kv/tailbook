import { resolveApiBaseUrl } from "./env";

export const CLIENT_TOKEN_KEY = "tailbook.client.accessToken";
export const CLIENT_REFRESH_TOKEN_KEY = "tailbook.client.refreshToken";
export const CLIENT_EMAIL_KEY = "tailbook.client.email";
export const CLIENT_UNAUTHORIZED_EVENT = "tailbook:client:unauthorized";

export function getStoredAccessToken(): string | null {
    if (typeof window === "undefined") {
        return null;
    }

    return window.localStorage.getItem(CLIENT_TOKEN_KEY);
}

export function getStoredRefreshToken(): string | null {
    if (typeof window === "undefined") {
        return null;
    }

    return window.localStorage.getItem(CLIENT_REFRESH_TOKEN_KEY);
}

export function storeSession(accessToken: string, email: string, refreshToken?: string | null) {
    if (typeof window === "undefined") {
        return;
    }

    window.localStorage.setItem(CLIENT_TOKEN_KEY, accessToken);
    if (refreshToken) {
        window.localStorage.setItem(CLIENT_REFRESH_TOKEN_KEY, refreshToken);
    }
    window.localStorage.setItem(CLIENT_EMAIL_KEY, email);
}

export function storeEmail(email: string) {
    if (typeof window === "undefined") {
        return;
    }

    window.localStorage.setItem(CLIENT_EMAIL_KEY, email);
}

export function clearSession() {
    if (typeof window === "undefined") {
        return;
    }

    window.localStorage.removeItem(CLIENT_TOKEN_KEY);
    window.localStorage.removeItem(CLIENT_REFRESH_TOKEN_KEY);
    window.localStorage.removeItem(CLIENT_EMAIL_KEY);
}

export async function revokeSession() {
    const refreshToken = getStoredRefreshToken();
    clearSession();

    if (!refreshToken) {
        return;
    }

    try {
        await fetch(`${resolveApiBaseUrl()}/api/client/auth/revoke`, {
            method: "POST",
            headers: {
                "Accept": "application/json",
                "Content-Type": "application/json"
            },
            body: JSON.stringify({ refreshToken }),
            cache: "no-store"
        });
    } catch {
        // Logout must still clear local state when the API is unavailable.
    }
}

export function notifyUnauthorized() {
    clearSession();
    if (typeof window === "undefined") {
        return;
    }

    window.dispatchEvent(new CustomEvent(CLIENT_UNAUTHORIZED_EVENT));
}

export function getStoredEmail(): string | null {
    if (typeof window === "undefined") {
        return null;
    }

    return window.localStorage.getItem(CLIENT_EMAIL_KEY);
}
