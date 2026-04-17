export const CLIENT_TOKEN_KEY = "tailbook.client.accessToken";
export const CLIENT_EMAIL_KEY = "tailbook.client.email";

export function getStoredAccessToken(): string | null {
    if (typeof window === "undefined") {
        return null;
    }

    return window.localStorage.getItem(CLIENT_TOKEN_KEY);
}

export function storeSession(accessToken: string, email: string) {
    if (typeof window === "undefined") {
        return;
    }

    window.localStorage.setItem(CLIENT_TOKEN_KEY, accessToken);
    window.localStorage.setItem(CLIENT_EMAIL_KEY, email);
}

export function clearSession() {
    if (typeof window === "undefined") {
        return;
    }

    window.localStorage.removeItem(CLIENT_TOKEN_KEY);
    window.localStorage.removeItem(CLIENT_EMAIL_KEY);
}

export function getStoredEmail(): string | null {
    if (typeof window === "undefined") {
        return null;
    }

    return window.localStorage.getItem(CLIENT_EMAIL_KEY);
}
