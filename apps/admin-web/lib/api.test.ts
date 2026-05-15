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

    it("dispatches request to correct URL with JSON headers", async () => {
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
        const mockResponse = new Response(JSON.stringify({}), { status: 200, headers: { "content-type": "application/json" } });
        const fetchSpy = vi.fn().mockResolvedValue(mockResponse);
        globalThis.fetch = fetchSpy;

        const { publicApiRequest } = await import("./api");
        await publicApiRequest("/api/test", { method: "POST", body: JSON.stringify({ key: "value" }) });

        const headers = new Headers(fetchSpy.mock.calls[0][1]?.headers ?? {});
        expect(headers.get("Content-Type")).toBe("application/json");
    });
});
