const ADMIN_TOKEN_KEY = "tailbook_admin_token";
const ADMIN_EMAIL_KEY = "tailbook_admin_email";
const ADMIN_DISPLAY_NAME_KEY = "tailbook_admin_display_name";

function canUseStorage() {
  return typeof window !== "undefined";
}

export function getAdminToken() {
  if (!canUseStorage()) return null;
  return window.localStorage.getItem(ADMIN_TOKEN_KEY);
}

export function setAdminSession(input: { accessToken: string; email: string; displayName: string }) {
  if (!canUseStorage()) return;
  window.localStorage.setItem(ADMIN_TOKEN_KEY, input.accessToken);
  window.localStorage.setItem(ADMIN_EMAIL_KEY, input.email);
  window.localStorage.setItem(ADMIN_DISPLAY_NAME_KEY, input.displayName);
}

export function clearAdminSession() {
  if (!canUseStorage()) return;
  window.localStorage.removeItem(ADMIN_TOKEN_KEY);
  window.localStorage.removeItem(ADMIN_EMAIL_KEY);
  window.localStorage.removeItem(ADMIN_DISPLAY_NAME_KEY);
}

export function getAdminEmail() {
  if (!canUseStorage()) return null;
  return window.localStorage.getItem(ADMIN_EMAIL_KEY);
}

export function getAdminDisplayName() {
  if (!canUseStorage()) return null;
  return window.localStorage.getItem(ADMIN_DISPLAY_NAME_KEY);
}
