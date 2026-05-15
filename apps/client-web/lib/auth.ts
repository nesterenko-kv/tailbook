import { resolveApiBaseUrl } from "./env";
import { createBrowserSessionRequestInit } from "@tailbook/frontend-api";

export const CLIENT_EMAIL_KEY = "tailbook.client.email";
export const CLIENT_UNAUTHORIZED_EVENT = "tailbook:client:unauthorized";
export const CLIENT_SESSION_SURFACE = "client";
export const CLIENT_CSRF_COOKIE_NAME = "__Host-tailbook-client-csrf";

// Access token stored in memory only (not localStorage) to minimize XSS exposure.
// Refresh token: in RefreshCookie mode (production) it lives in an HttpOnly cookie;
// in BodyTokens mode (local dev) it is also kept in memory.
// On page reload the access token is null; apiRequest catches 401 and refreshes
// using the HttpOnly refresh cookie (or re-login if the session expired).
let accessToken: string | null = null;
let legacyRefreshToken: string | null = null;

export function getStoredAccessToken(): string | null {
    return accessToken;
}

export function getStoredRefreshToken(): string | null {
    return legacyRefreshToken;
}

export function createClientBrowserSessionRequest(init: RequestInit = {}) {
    return createBrowserSessionRequestInit({
        surface: CLIENT_SESSION_SURFACE,
        csrfCookieName: CLIENT_CSRF_COOKIE_NAME
    }, init);
}

export function storeSession(accessToken: string, email: string, refreshToken?: string | null) {
    legacyRefreshToken = refreshToken ?? null;
    setAccessToken(accessToken);
    if (typeof window === "undefined") {
        return;
    }
    window.localStorage.setItem(CLIENT_EMAIL_KEY, email);
}

function setAccessToken(value: string | null) {
    accessToken = value;
}

export function storeEmail(email: string) {
    if (typeof window === "undefined") {
        return;
    }
    window.localStorage.setItem(CLIENT_EMAIL_KEY, email);
}

export function clearSession() {
    setAccessToken(null);
    legacyRefreshToken = null;
    if (typeof window === "undefined") {
        return;
    }
    window.localStorage.removeItem(CLIENT_EMAIL_KEY);
}

export async function revokeSession() {
    const refreshToken = getStoredRefreshToken();
    clearSession();
    try {
        await fetch(`${resolveApiBaseUrl()}/api/client/auth/revoke`, {
            ...createClientBrowserSessionRequest({
                method: "POST",
                headers: {
                    "Accept": "application/json",
                    "Content-Type": "application/json"
                },
                body: JSON.stringify(refreshToken ? { refreshToken } : {})
            }),
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
