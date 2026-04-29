export type ApiRequestConfig<TError extends Error> = {
    resolveApiBaseUrl: () => string;
    getAccessToken: () => string | null;
    refreshSession?: () => Promise<boolean>;
    notifyUnauthorized: () => void;
    shouldAttemptRefresh?: (path: string, init?: RequestInit) => boolean;
    getErrorMessage: (payload: unknown, response: Response) => string;
    createError: (message: string, status: number, payload: unknown) => TError;
};

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

export function createApiRequest<TError extends Error>(config: ApiRequestConfig<TError>) {
    async function send(path: string, init?: RequestInit) {
        return fetch(resolveUrl(path, config.resolveApiBaseUrl()), {
            ...init,
            headers: buildHeaders(init, config.getAccessToken()),
            cache: "no-store"
        });
    }

    return async function apiRequest<T>(path: string, init?: RequestInit): Promise<T> {
        let response = await send(path, init);
        const shouldRefresh = config.shouldAttemptRefresh?.(path, init) ?? false;

        if (response.status === 401 && shouldRefresh && await config.refreshSession?.()) {
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
