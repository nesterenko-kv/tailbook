import { describe, it, expect, vi, afterEach } from "vitest";
import { ApiError } from "./api";

describe("ApiError", () => {
    it("creates error with message and status", () => {
        const error = new ApiError("Not found", 404);
        expect(error.message).toBe("Not found");
        expect(error.status).toBe(404);
        expect(error.name).toBe("ApiError");
    });

    it("creates error with details", () => {
        const details = { field: "email" };
        const error = new ApiError("Validation failed", 400, details);
        expect(error.details).toEqual(details);
    });

    it("is instance of Error", () => {
        const error = new ApiError("Test", 500);
        expect(error).toBeInstanceOf(Error);
    });
});

describe("publicApiRequest", () => {
    const originalFetch = globalThis.fetch;

    afterEach(() => {
        globalThis.fetch = originalFetch;
    });

    it("dispatches request to the correct URL with JSON headers", async () => {
        const mockResponse = new Response(JSON.stringify({ status: "ok" }), {
            status: 200,
            headers: { "content-type": "application/json" }
        });
        const fetchSpy = vi.fn().mockResolvedValue(mockResponse);
        globalThis.fetch = fetchSpy;

        const { publicApiRequest } = await import("./api");

        const result = await publicApiRequest<{ status: string }>("/api/identity/auth/login");

        expect(result.status).toBe("ok");
        expect(fetchSpy).toHaveBeenCalledTimes(1);

        const callUrl = fetchSpy.mock.calls[0][0];
        expect(callUrl).toContain("/api/identity/auth/login");

        const callInit = fetchSpy.mock.calls[0][1];
        const headers = new Headers(callInit?.headers ?? {});
        expect(headers.get("Accept")).toBe("application/json");
    });

    it("rejects with ApiError on non-ok response", async () => {
        const mockResponse = new Response(JSON.stringify({ message: "Bad request" }), {
            status: 400,
            headers: { "content-type": "application/json" }
        });
        globalThis.fetch = vi.fn().mockResolvedValue(mockResponse);

        const { publicApiRequest } = await import("./api");

        await expect(publicApiRequest("/api/test")).rejects.toThrow(ApiError);
    });

    it("includes Content-Type for requests with body", async () => {
        const mockResponse = new Response(JSON.stringify({ status: "ok" }), {
            status: 200,
            headers: { "content-type": "application/json" }
        });
        const fetchSpy = vi.fn().mockResolvedValue(mockResponse);
        globalThis.fetch = fetchSpy;

        const { publicApiRequest } = await import("./api");
        await publicApiRequest("/api/test", {
            method: "POST",
            body: JSON.stringify({ key: "value" })
        });

        const callInit = fetchSpy.mock.calls[0][1];
        const headers = new Headers(callInit?.headers ?? {});
        expect(headers.get("Content-Type")).toBe("application/json");
    });
});

describe("apiRequest", () => {
    const originalFetch = globalThis.fetch;

    afterEach(() => {
        globalThis.fetch = originalFetch;
    });

    it("includes Authorization header from stored token", async () => {
        const mockResponse = new Response(JSON.stringify({ id: 1 }), {
            status: 200,
            headers: { "content-type": "application/json" }
        });
        const fetchSpy = vi.fn().mockResolvedValue(mockResponse);
        globalThis.fetch = fetchSpy;

        const { storeSession, getStoredAccessToken } = await import("./auth");
        storeSession("test-token", "test@test.com", "Test Groomer");

        expect(getStoredAccessToken()).toBe("test-token");
    });

    it("notifies unauthorized on 401 response", async () => {
        const mockResponse = new Response(JSON.stringify({ message: "Unauthorized" }), {
            status: 401,
            headers: { "content-type": "application/json" }
        });
        globalThis.fetch = vi.fn().mockResolvedValue(mockResponse);

        const { apiRequest, ApiError } = await import("./api");

        await expect(apiRequest("/api/me")).rejects.toThrow(ApiError);
        try {
            await apiRequest("/api/me");
        } catch (err) {
            if (err instanceof ApiError) {
                expect(err.status).toBe(401);
            }
        }
    });
});
