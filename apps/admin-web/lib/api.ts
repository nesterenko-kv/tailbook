import { ApiError, createApiError, createApiRequest, createImplicitAccessTokenPolicy, createPublicApiRequest, getApiErrorMessage } from "@tailbook/frontend-api";
import { resolveApiBaseUrl } from "@/lib/env";
import { createAdminBrowserSessionRequest, getAdminRefreshToken, getAdminToken, notifyAdminUnauthorized, setAdminSession } from "@/lib/auth";

export { ApiError };

type RefreshResponse = {
  accessToken: string;
  refreshToken?: string | null;
  user: {
    email: string;
    displayName: string;
  };
};

const implicitAccessTokenPolicy = createImplicitAccessTokenPolicy({
  resolveApiBaseUrl,
  pathsWithoutImplicitAccessToken: [
    "/api/identity/auth/login",
    "/api/identity/auth/mfa/verify",
    "/api/identity/auth/mfa/recovery-code/verify",
    "/api/identity/auth/refresh",
    "/api/identity/auth/revoke"
  ]
});

async function refreshAdminSession() {
  const refreshToken = getAdminRefreshToken();

  const response = await fetch(`${resolveApiBaseUrl()}/api/identity/auth/refresh`, {
    ...createAdminBrowserSessionRequest({
      method: "POST",
      headers: {
        "Accept": "application/json",
        "Content-Type": "application/json"
      },
      body: JSON.stringify(refreshToken ? { refreshToken } : {})
    }),
    cache: "no-store"
  }).catch(() => null);

  if (!response?.ok) {
    notifyAdminUnauthorized();
    return false;
  }

  const payload = await response.json().catch(() => null) as RefreshResponse | null;
  if (!payload?.accessToken || !payload.user?.email) {
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

export const apiRequest = createApiRequest<ApiError>({
  resolveApiBaseUrl,
  getAccessToken: getAdminToken,
  refreshSession: refreshAdminSession,
  notifyUnauthorized: notifyAdminUnauthorized,
  ...implicitAccessTokenPolicy,
  getErrorMessage: getApiErrorMessage,
  createError: createApiError
});

export const publicApiRequest = createPublicApiRequest<ApiError>({
  resolveApiBaseUrl,
  getErrorMessage: getApiErrorMessage,
  createError: createApiError
});
