import { resolveApiBaseUrl } from "@/lib/env";

const ADMIN_TOKEN_KEY = "tailbook_admin_token";
const ADMIN_REFRESH_TOKEN_KEY = "tailbook_admin_refresh_token";
const ADMIN_EMAIL_KEY = "tailbook_admin_email";
const ADMIN_DISPLAY_NAME_KEY = "tailbook_admin_display_name";
export const ADMIN_UNAUTHORIZED_EVENT = "tailbook:admin:unauthorized";

function canUseStorage() {
  return typeof window !== "undefined";
}

export function getAdminToken() {
  if (!canUseStorage()) return null;
  return window.localStorage.getItem(ADMIN_TOKEN_KEY);
}

export function getAdminRefreshToken() {
  if (!canUseStorage()) return null;
  return window.localStorage.getItem(ADMIN_REFRESH_TOKEN_KEY);
}

export function setAdminSession(input: { accessToken: string; refreshToken?: string | null; email: string; displayName: string }) {
  if (!canUseStorage()) return;
  window.localStorage.setItem(ADMIN_TOKEN_KEY, input.accessToken);
  if (input.refreshToken) {
    window.localStorage.setItem(ADMIN_REFRESH_TOKEN_KEY, input.refreshToken);
  }
  setAdminProfile({ email: input.email, displayName: input.displayName });
}

export function setAdminProfile(input: { email: string; displayName: string }) {
  if (!canUseStorage()) return;
  window.localStorage.setItem(ADMIN_EMAIL_KEY, input.email);
  window.localStorage.setItem(ADMIN_DISPLAY_NAME_KEY, input.displayName);
}

export function clearAdminSession() {
  if (!canUseStorage()) return;
  window.localStorage.removeItem(ADMIN_TOKEN_KEY);
  window.localStorage.removeItem(ADMIN_REFRESH_TOKEN_KEY);
  window.localStorage.removeItem(ADMIN_EMAIL_KEY);
  window.localStorage.removeItem(ADMIN_DISPLAY_NAME_KEY);
}

export async function revokeAdminSession() {
  const refreshToken = getAdminRefreshToken();
  clearAdminSession();

  if (!refreshToken) {
    return;
  }

  try {
    await fetch(`${resolveApiBaseUrl()}/api/identity/auth/revoke`, {
      method: "POST",
      headers: {
        "Accept": "application/json",
        "Content-Type": "application/json"
      },
      body: JSON.stringify({ refreshToken }),
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
