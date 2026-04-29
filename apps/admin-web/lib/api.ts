import { createApiRequest } from "@tailbook/frontend-api";
import { resolveApiBaseUrl } from "@/lib/env";
import { getAdminRefreshToken, getAdminToken, notifyAdminUnauthorized, setAdminSession } from "@/lib/auth";

export class ApiError extends Error {
  status: number;
  details?: unknown;

  constructor(message: string, status: number, details?: unknown) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.details = details;
  }
}

type RefreshResponse = {
  accessToken: string;
  refreshToken: string;
  user: {
    email: string;
    displayName: string;
  };
};

function shouldAttemptRefresh(path: string, init?: RequestInit) {
  if (typeof window === "undefined") return false;
  if (new Headers(init?.headers ?? {}).has("Authorization")) return false;
  return !path.includes("/api/identity/auth/login")
    && !path.includes("/api/identity/auth/refresh")
    && !path.includes("/api/identity/auth/revoke");
}

async function refreshAdminSession() {
  const refreshToken = getAdminRefreshToken();
  if (!refreshToken) return false;

  const response = await fetch(`${resolveApiBaseUrl()}/api/identity/auth/refresh`, {
    method: "POST",
    headers: {
      "Accept": "application/json",
      "Content-Type": "application/json"
    },
    body: JSON.stringify({ refreshToken }),
    cache: "no-store"
  }).catch(() => null);

  if (!response?.ok) {
    notifyAdminUnauthorized();
    return false;
  }

  const payload = await response.json().catch(() => null) as RefreshResponse | null;
  if (!payload?.accessToken || !payload.refreshToken || !payload.user?.email) {
    notifyAdminUnauthorized();
    return false;
  }

  setAdminSession({
    accessToken: payload.accessToken,
    refreshToken: payload.refreshToken,
    email: payload.user.email,
    displayName: payload.user.displayName
  });
  return true;
}

function getErrorMessage(payload: unknown, response: Response) {
  const generalErrors = payload && typeof payload === "object" && "errors" in payload && payload.errors && typeof payload.errors === "object" && "generalErrors" in payload.errors
    ? (payload.errors as { generalErrors?: string[] }).generalErrors
    : undefined;
  const title = payload && typeof payload === "object" && "title" in payload ? String(payload.title) : undefined;
  const text = typeof payload === "string" && payload.length > 0 ? payload : undefined;
  return generalErrors?.[0] ?? title ?? text ?? response.statusText ?? "Request failed";
}

export const apiRequest = createApiRequest<ApiError>({
  resolveApiBaseUrl,
  getAccessToken: getAdminToken,
  refreshSession: refreshAdminSession,
  notifyUnauthorized: notifyAdminUnauthorized,
  shouldAttemptRefresh,
  getErrorMessage,
  createError: (message, status, payload) => new ApiError(message, status, payload)
});
