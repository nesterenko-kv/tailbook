export const GROOMER_TOKEN_KEY = "tailbook.groomer.accessToken";
export const GROOMER_EMAIL_KEY = "tailbook.groomer.email";

export function getStoredAccessToken(): string | null {
    if (typeof window === "undefined") {
        return null;
    }

    return window.localStorage.getItem(GROOMER_TOKEN_KEY);
}

export function storeSession(accessToken: string, email: string) {
    if (typeof window === "undefined") {
        return;
    }

    window.localStorage.setItem(GROOMER_TOKEN_KEY, accessToken);
    window.localStorage.setItem(GROOMER_EMAIL_KEY, email);
}

export function clearSession() {
    if (typeof window === "undefined") {
        return;
    }

    window.localStorage.removeItem(GROOMER_TOKEN_KEY);
    window.localStorage.removeItem(GROOMER_EMAIL_KEY);
}

export function getStoredEmail(): string | null {
    if (typeof window === "undefined") {
        return null;
    }

    return window.localStorage.getItem(GROOMER_EMAIL_KEY);
}
