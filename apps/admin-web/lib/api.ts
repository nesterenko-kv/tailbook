import { env } from "@/lib/env";
import { getAdminToken } from "@/lib/auth";

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

function normalizeUrl(path: string) {
  if (path.startsWith("http://") || path.startsWith("https://")) {
    return path;
  }
  return `${env.apiBaseUrl}${path}`;
}

export async function apiRequest<T>(path: string, init?: RequestInit): Promise<T> {
  const token = getAdminToken();
  const headers = new Headers(init?.headers ?? {});
  headers.set("Accept", "application/json");

  if (init?.body && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  if (token && !headers.has("Authorization")) {
    headers.set("Authorization", `Bearer ${token}`);
  }

  const response = await fetch(normalizeUrl(path), {
    ...init,
    headers,
    cache: "no-store"
  });

  const contentType = response.headers.get("content-type") ?? "";
  const isJson = contentType.includes("application/json") || contentType.includes("problem+json");
  const payload = isJson ? await response.json().catch(() => null) : await response.text().catch(() => null);

  if (!response.ok) {
    const generalErrors = payload && typeof payload === "object" && "errors" in payload && payload.errors && typeof payload.errors === "object" && "generalErrors" in payload.errors
      ? (payload.errors as { generalErrors?: string[] }).generalErrors
      : undefined;
    const title = payload && typeof payload === "object" && "title" in payload ? String(payload.title) : undefined;
    const message = generalErrors?.[0] ?? title ?? response.statusText ?? "Request failed";
    throw new ApiError(message, response.status, payload);
  }

  return payload as T;
}
