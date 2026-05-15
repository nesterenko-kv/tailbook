import { resolveApiBaseUrl } from "./env";
import { createBrowserSessionRequestInit } from "@tailbook/frontend-api";

export const GROOMER_EMAIL_KEY = "tailbook.groomer.email";
export const GROOMER_DISPLAY_NAME_KEY = "tailbook.groomer.displayName";
export const GROOMER_UNAUTHORIZED_EVENT = "tailbook:groomer:unauthorized";
export const GROOMER_SESSION_SURFACE = "groomer";
export const GROOMER_CSRF_COOKIE_NAME = "__Host-tailbook-groomer-csrf";

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

export function createGroomerBrowserSessionRequest(init: RequestInit = {}) {
    return createBrowserSessionRequestInit({
        surface: GROOMER_SESSION_SURFACE,
        csrfCookieName: GROOMER_CSRF_COOKIE_NAME
    }, init);
}

export function storeSession(accessToken: string, email: string, displayName?: string, refreshToken?: string | null) {
    setAccessToken(accessToken);
    legacyRefreshToken = refreshToken ?? null;
    if (typeof window === "undefined") {
        return;
    }
    window.localStorage.setItem(GROOMER_EMAIL_KEY, email);
    if (displayName) {
        window.localStorage.setItem(GROOMER_DISPLAY_NAME_KEY, displayName);
    }
}

function setAccessToken(value: string | null) {
    accessToken = value;
}

export function storeProfile(email: string, displayName: string) {
    if (typeof window === "undefined") {
        return;
    }
    window.localStorage.setItem(GROOMER_EMAIL_KEY, email);
    window.localStorage.setItem(GROOMER_DISPLAY_NAME_KEY, displayName);
}

export function clearSession() {
    setAccessToken(null);
    legacyRefreshToken = null;
    if (typeof window === "undefined") {
        return;
    }
    window.localStorage.removeItem(GROOMER_EMAIL_KEY);
    window.localStorage.removeItem(GROOMER_DISPLAY_NAME_KEY);
}

export async function revokeSession() {
    clearSession();
    try {
        await fetch(`${resolveApiBaseUrl()}/api/identity/auth/revoke`, {
            ...createGroomerBrowserSessionRequest({
                method: "POST",
                headers: {
                    "Accept": "application/json",
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({})
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
    window.dispatchEvent(new CustomEvent(GROOMER_UNAUTHORIZED_EVENT));
}

export function getStoredEmail(): string | null {
    if (typeof window === "undefined") {
        return null;
    }
    return window.localStorage.getItem(GROOMER_EMAIL_KEY);
}

export function getStoredDisplayName(): string | null {
    if (typeof window === "undefined") {
        return null;
    }
    return window.localStorage.getItem(GROOMER_DISPLAY_NAME_KEY);
}
