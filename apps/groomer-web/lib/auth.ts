export const GROOMER_TOKEN_KEY = "tailbook.groomer.accessToken";
export const GROOMER_EMAIL_KEY = "tailbook.groomer.email";
export const GROOMER_DISPLAY_NAME_KEY = "tailbook.groomer.displayName";
export const GROOMER_UNAUTHORIZED_EVENT = "tailbook:groomer:unauthorized";

export function getStoredAccessToken(): string | null {
    if (typeof window === "undefined") {
        return null;
    }

    return window.localStorage.getItem(GROOMER_TOKEN_KEY);
}

export function storeSession(accessToken: string, email: string, displayName?: string) {
    if (typeof window === "undefined") {
        return;
    }

    window.localStorage.setItem(GROOMER_TOKEN_KEY, accessToken);
    window.localStorage.setItem(GROOMER_EMAIL_KEY, email);
    if (displayName) {
        window.localStorage.setItem(GROOMER_DISPLAY_NAME_KEY, displayName);
    }
}

export function storeProfile(email: string, displayName: string) {
    if (typeof window === "undefined") {
        return;
    }

    window.localStorage.setItem(GROOMER_EMAIL_KEY, email);
    window.localStorage.setItem(GROOMER_DISPLAY_NAME_KEY, displayName);
}

export function clearSession() {
    if (typeof window === "undefined") {
        return;
    }

    window.localStorage.removeItem(GROOMER_TOKEN_KEY);
    window.localStorage.removeItem(GROOMER_EMAIL_KEY);
    window.localStorage.removeItem(GROOMER_DISPLAY_NAME_KEY);
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
