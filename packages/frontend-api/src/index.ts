export type ApiBaseUrlResolverConfig = {
    publicApiBaseUrl?: string;
    internalApiBaseUrl?: string;
    defaultPublicApiBaseUrl?: string;
};

export class ApiError extends Error {
    status: number;
    details?: unknown;

    constructor(message: string, status: number, details?: unknown) {
        super(message);
        this.name = "ApiError";
        this.status = status;
        this.details = details;
        Object.setPrototypeOf(this, new.target.prototype);
    }
}

export function createApiError(message: string, status: number, payload: unknown) {
    return new ApiError(message, status, payload);
}

export function createApiBaseUrlResolver(config: ApiBaseUrlResolverConfig = {}) {
    const publicApiBaseUrl = config.publicApiBaseUrl ?? config.defaultPublicApiBaseUrl ?? "https://localhost:5001";
    const internalApiBaseUrl = config.internalApiBaseUrl ?? publicApiBaseUrl;

    return {
        publicApiBaseUrl,
        internalApiBaseUrl,
        resolveApiBaseUrl() {
            return typeof window === "undefined" ? internalApiBaseUrl : publicApiBaseUrl;
        }
    };
}

export type ApiRequestConfig<TError extends Error> = {
    resolveApiBaseUrl: () => string;
    getAccessToken: () => string | null;
    refreshSession?: () => Promise<boolean>;
    notifyUnauthorized: () => void;
    shouldAttachAccessToken?: (path: string, init?: RequestInit) => boolean;
    shouldAttemptRefresh?: (path: string, init?: RequestInit) => boolean;
    getErrorMessage: (payload: unknown, response: Response) => string;
    createError: (message: string, status: number, payload: unknown) => TError;
};

export type PublicApiRequestConfig<TError extends Error> = Pick<
    ApiRequestConfig<TError>,
    "resolveApiBaseUrl" | "getErrorMessage" | "createError"
>;

export type ImplicitAccessTokenPolicyConfig = {
    resolveApiBaseUrl: () => string;
    pathsWithoutImplicitAccessToken: readonly string[];
};

export type BrowserSessionSurface = "admin" | "groomer" | "client";

export type BrowserSessionRequestConfig = {
    surface: BrowserSessionSurface;
    csrfCookieName: string;
    surfaceHeaderName?: string;
    csrfHeaderName?: string;
};

function normalizePathname(pathname: string) {
    const normalized = pathname.split("?")[0].replace(/\/+$/, "");
    return normalized === "" ? "/" : normalized;
}

function resolveRequestPathname(path: string, resolveApiBaseUrl: () => string) {
    try {
        return normalizePathname(new URL(path, resolveApiBaseUrl()).pathname);
    } catch {
        return normalizePathname(path);
    }
}

function hasExplicitAuthorizationHeader(init?: RequestInit) {
    return new Headers(init?.headers ?? {}).has("Authorization");
}

export function getBrowserCookieValue(name: string) {
    if (typeof document === "undefined") {
        return null;
    }

    const prefix = `${name}=`;
    const cookie = document.cookie
        .split(";")
        .map((part) => part.trim())
        .find((part) => part.startsWith(prefix));

    if (!cookie) {
        return null;
    }

    const value = cookie.slice(prefix.length);
    try {
        return decodeURIComponent(value);
    } catch {
        return value;
    }
}

export function createBrowserSessionRequestInit(
    config: BrowserSessionRequestConfig,
    init: RequestInit = {}
): RequestInit {
    const headers = new Headers(init.headers ?? {});
    headers.set(config.surfaceHeaderName ?? "X-Tailbook-Session-Surface", config.surface);

    const csrfToken = getBrowserCookieValue(config.csrfCookieName);
    if (csrfToken) {
        headers.set(config.csrfHeaderName ?? "X-Tailbook-CSRF", csrfToken);
    }

    return {
        ...init,
        credentials: "include",
        headers
    };
}

export function createImplicitAccessTokenPolicy(config: ImplicitAccessTokenPolicyConfig) {
    const pathsWithoutImplicitAccessToken = new Set(
        config.pathsWithoutImplicitAccessToken.map(normalizePathname)
    );

    function isPathWithoutImplicitAccessToken(path: string) {
        return pathsWithoutImplicitAccessToken.has(
            resolveRequestPathname(path, config.resolveApiBaseUrl)
        );
    }

    return {
        shouldAttachAccessToken(path: string) {
            return !isPathWithoutImplicitAccessToken(path);
        },
        shouldAttemptRefresh(path: string, init?: RequestInit) {
            if (typeof window === "undefined") return false;
            if (hasExplicitAuthorizationHeader(init)) return false;
            return !isPathWithoutImplicitAccessToken(path);
        }
    };
}

export function getApiErrorMessage(payload: unknown, response: Response) {
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
    return response.statusText || `Request failed with status ${response.status}`;
}

function resolveUrl(path: string, baseUrl: string) {
    if (path.startsWith("http://") || path.startsWith("https://")) {
        return path;
    }

    const normalizedBaseUrl = baseUrl.replace(/\/+$/, "");
    const normalizedPath = path.startsWith("/") ? path : `/${path}`;
    return `${normalizedBaseUrl}${normalizedPath}`;
}

function buildHeaders(init: RequestInit | undefined, accessToken: string | null) {
    const headers = new Headers(init?.headers ?? {});
    headers.set("Accept", "application/json");

    if (init?.body && !headers.has("Content-Type")) {
        headers.set("Content-Type", "application/json");
    }

    if (accessToken && !headers.has("Authorization")) {
        headers.set("Authorization", `Bearer ${accessToken}`);
    }

    return headers;
}

async function parsePayload(response: Response) {
    if (response.status === 204) {
        return undefined;
    }

    const contentType = response.headers.get("content-type") ?? "";
    const isJson = contentType.includes("application/json") || contentType.includes("problem+json");

    if (isJson) {
        return await response.json().catch(() => null);
    }

    const text = await response.text().catch(() => null);
    return text === "" ? null : text;
}

export function createPublicApiRequest<TError extends Error>(config: PublicApiRequestConfig<TError>) {
    return createApiRequest<TError>({
        ...config,
        getAccessToken: () => null,
        notifyUnauthorized: () => undefined,
        shouldAttemptRefresh: () => false
    });
}

export function createApiRequest<TError extends Error>(config: ApiRequestConfig<TError>) {
    let refreshPromise: Promise<boolean> | null = null;

    function getAccessTokenForRequest(path: string, init?: RequestInit) {
        const shouldAttachAccessToken = config.shouldAttachAccessToken?.(path, init) ?? true;
        return shouldAttachAccessToken ? config.getAccessToken() : null;
    }

    async function refreshSessionOnce() {
        if (!config.refreshSession) return false;

        refreshPromise ??= config.refreshSession()
            .catch(() => false)
            .finally(() => {
                refreshPromise = null;
            });

        return refreshPromise;
    }

    async function send(path: string, init?: RequestInit) {
        return fetch(resolveUrl(path, config.resolveApiBaseUrl()), {
            ...init,
            headers: buildHeaders(init, getAccessTokenForRequest(path, init)),
            cache: "no-store"
        });
    }

    return async function apiRequest<T>(path: string, init?: RequestInit): Promise<T> {
        let response = await send(path, init);
        const shouldRefresh = config.shouldAttemptRefresh?.(path, init) ?? false;

        if (response.status === 401 && shouldRefresh && await refreshSessionOnce()) {
            response = await send(path, init);
        }

        const payload = await parsePayload(response);

        if (!response.ok) {
            if (response.status === 401) {
                config.notifyUnauthorized();
            }

            throw config.createError(
                config.getErrorMessage(payload, response),
                response.status,
                payload
            );
        }

        return payload as T;
    };
}
