import { resolveApiBaseUrl } from "./env";
import { getStoredAccessToken } from "./auth";

export class ApiError extends Error {
    status: number;

    constructor(status: number, message: string) {
        super(message);
        this.status = status;
    }
}

export async function apiRequest<T>(path: string, init?: RequestInit): Promise<T> {
    const headers = new Headers(init?.headers ?? {});
    headers.set("Accept", "application/json");

    const token = getStoredAccessToken();
    if (token) {
        headers.set("Authorization", `Bearer ${token}`);
    }

    if (init?.body && !headers.has("Content-Type")) {
        headers.set("Content-Type", "application/json");
    }

    const response = await fetch(`${resolveApiBaseUrl()}${path}`, {
        ...init,
        headers,
        cache: "no-store"
    });

    if (!response.ok) {
        let message = `Request failed with status ${response.status}`;
        try {
            const payload = await response.json();
            if (payload?.message) {
                message = payload.message;
            } else if (Array.isArray(payload?.errors) && payload.errors.length > 0) {
                message = payload.errors.join(" ");
            } else if (payload?.errors?.generalErrors?.length > 0) {
                message = payload.errors.generalErrors.join(" ");
            }
        } catch {
            // ignore parse failures
        }

        throw new ApiError(response.status, message);
    }

    if (response.status === 204) {
        return undefined as T;
    }

    return (await response.json()) as T;
}
