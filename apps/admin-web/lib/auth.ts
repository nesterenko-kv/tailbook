import { resolveApiBaseUrl } from "@/lib/env";
import { createBrowserSessionRequestInit } from "@tailbook/frontend-api";

const ADMIN_EMAIL_KEY = "tailbook_admin_email";
const ADMIN_DISPLAY_NAME_KEY = "tailbook_admin_display_name";
export const ADMIN_UNAUTHORIZED_EVENT = "tailbook:admin:unauthorized";
export const ADMIN_SESSION_SURFACE = "admin";
export const ADMIN_CSRF_COOKIE_NAME = "__Host-tailbook-admin-csrf";

// Access token stored in memory only (not localStorage) to minimize XSS exposure.
// Refresh token: in RefreshCookie mode (production) it lives in an HttpOnly cookie;
// in BodyTokens mode (local dev) it is also kept in memory.
// On page reload the access token is null; apiRequest catches 401 and refreshes
// using the HttpOnly refresh cookie (or re-login if the session expired).
let adminAccessToken: string | null = null;
let adminLegacyRefreshToken: string | null = null;

function canUseStorage() {
  return typeof window !== "undefined";
}

export function getAdminToken() {
  return adminAccessToken;
}

export function getAdminRefreshToken() {
  return adminLegacyRefreshToken;
}

export function createAdminBrowserSessionRequest(init: RequestInit = {}) {
  return createBrowserSessionRequestInit({
    surface: ADMIN_SESSION_SURFACE,
    csrfCookieName: ADMIN_CSRF_COOKIE_NAME
  }, init);
}

export function setAdminSession(input: { accessToken: string; refreshToken?: string | null; email: string; displayName: string }) {
  adminAccessToken = input.accessToken;
  adminLegacyRefreshToken = input.refreshToken ?? null;
  if (!canUseStorage()) return;
  setAdminProfile({ email: input.email, displayName: input.displayName });
}

export function setAdminProfile(input: { email: string; displayName: string }) {
  if (!canUseStorage()) return;
  window.localStorage.setItem(ADMIN_EMAIL_KEY, input.email);
  window.localStorage.setItem(ADMIN_DISPLAY_NAME_KEY, input.displayName);
}

export function clearAdminSession() {
  adminAccessToken = null;
  adminLegacyRefreshToken = null;
  if (!canUseStorage()) return;
  window.localStorage.removeItem(ADMIN_EMAIL_KEY);
  window.localStorage.removeItem(ADMIN_DISPLAY_NAME_KEY);
}

export async function revokeAdminSession() {
  clearAdminSession();
  try {
    await fetch(`${resolveApiBaseUrl()}/api/identity/auth/revoke`, {
      ...createAdminBrowserSessionRequest({
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

export function notifyAdminUnauthorized() {
  clearAdminSession();
  if (!canUseStorage()) return;
  window.dispatchEvent(new CustomEvent(ADMIN_UNAUTHORIZED_EVENT));
}

export function getAdminEmail() {
  if (!canUseStorage()) return null;
  return window.localStorage.getItem(ADMIN_EMAIL_KEY);
}

export function getAdminDisplayName() {
  if (!canUseStorage()) return null;
  return window.localStorage.getItem(ADMIN_DISPLAY_NAME_KEY);
}
