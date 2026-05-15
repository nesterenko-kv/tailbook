import { describe, it, expect, vi, beforeEach } from "vitest";
import {
    ApiError,
    createApiError,
    createApiRequest,
    createPublicApiRequest,
    createImplicitAccessTokenPolicy,
    getApiErrorMessage,
    createApiBaseUrlResolver
} from "./index";

function mockJsonResponse(status: number, body: unknown, contentType = "application/json") {
    return {
        ok: status >= 200 && status < 300,
        status,
        headers: new Headers({ "content-type": contentType }),
        json: () => Promise.resolve(body),
        text: () => Promise.resolve(typeof body === "string" ? body : JSON.stringify(body))
    } as Response;
}

function mockTextResponse(status: number, text: string) {
    return {
        ok: status >= 200 && status < 300,
        status,
        headers: new Headers({ "content-type": "text/plain" }),
        json: () => Promise.reject(new Error("not json")),
        text: () => Promise.resolve(text)
    } as Response;
}

// ── ApiError ──────────────────────────────────────────────────
describe("ApiError", () => {
    it("creates error with message and status", () => {
        const err = new ApiError("not found", 404, { id: "x" });
        expect(err).toBeInstanceOf(Error);
        expect(err.name).toBe("ApiError");
        expect(err.message).toBe("not found");
        expect(err.status).toBe(404);
        expect(err.details).toEqual({ id: "x" });
    });

    it("createApiError is a convenience wrapper", () => {
        const err = createApiError("bad request", 400, null);
        expect(err).toBeInstanceOf(ApiError);
        expect(err.status).toBe(400);
    });
});

// ── getApiErrorMessage ────────────────────────────────────────
describe("getApiErrorMessage", () => {
    const res = { statusText: "Forbidden" } as Response;

    it("extracts message string from body", () => {
        expect(getApiErrorMessage({ message: "invalid" }, res)).toBe("invalid");
    });

    it("joins errors array", () => {
        expect(getApiErrorMessage({ errors: ["err1", "err2"] }, res)).toBe("err1 err2");
    });

    it("extracts generalErrors from nested errors object", () => {
        expect(getApiErrorMessage({ errors: { generalErrors: ["gen1", "gen2"] } }, res)).toBe("gen1 gen2");
    });

    it("extracts title field", () => {
        expect(getApiErrorMessage({ title: "Validation Failed" }, res)).toBe("Validation Failed");
    });

    it("returns string payload directly", () => {
        expect(getApiErrorMessage("raw error", res)).toBe("raw error");
    });

    it("falls back to statusText for empty body", () => {
        expect(getApiErrorMessage({}, { statusText: "Not Found" } as Response)).toBe("Not Found");
    });

    it("falls back to generic message when nothing available", () => {
        expect(getApiErrorMessage(null, { status: 500, statusText: "" } as Response)).toBe("Request failed with status 500");
    });
});

// ── createImplicitAccessTokenPolicy ──────────────────────────
describe("createImplicitAccessTokenPolicy", () => {
    const policy = createImplicitAccessTokenPolicy({
        resolveApiBaseUrl: () => "https://api.test",
        pathsWithoutImplicitAccessToken: ["/api/auth/login", "/api/auth/refresh"]
    });

    it("attaches token for normal paths", () => {
        expect(policy.shouldAttachAccessToken("/api/data")).toBe(true);
    });

    it("attempts refresh for normal paths in browser", () => {
        vi.stubGlobal("window", {});
        const policy2 = createImplicitAccessTokenPolicy({
            resolveApiBaseUrl: () => "https://api.test",
            pathsWithoutImplicitAccessToken: ["/api/auth/login", "/api/auth/refresh"]
        });
        expect(policy2.shouldAttemptRefresh("/api/data")).toBe(true);
        vi.unstubAllGlobals();
    });

    it("skips token for login", () => {
        expect(policy.shouldAttachAccessToken("/api/auth/login")).toBe(false);
    });

    it("skips refresh for login paths in browser", () => {
        vi.stubGlobal("window", {});
        const policy2 = createImplicitAccessTokenPolicy({
            resolveApiBaseUrl: () => "https://api.test",
            pathsWithoutImplicitAccessToken: ["/api/auth/login", "/api/auth/refresh"]
        });
        expect(policy2.shouldAttemptRefresh("/api/auth/login")).toBe(false);
        vi.unstubAllGlobals();
    });

    it("skips token for refresh", () => {
        expect(policy.shouldAttachAccessToken("/api/auth/refresh")).toBe(false);
    });

    it("normalizes trailing slashes", () => {
        expect(policy.shouldAttachAccessToken("/api/auth/login/")).toBe(false);
    });

    it("does not attempt refresh when Authorization header is present", () => {
        vi.stubGlobal("window", {});
        const policy2 = createImplicitAccessTokenPolicy({
            resolveApiBaseUrl: () => "https://api.test",
            pathsWithoutImplicitAccessToken: ["/api/auth/login", "/api/auth/refresh"]
        });
        expect(policy2.shouldAttemptRefresh("/api/data", { headers: { Authorization: "Bearer x" } })).toBe(false);
        vi.unstubAllGlobals();
    });
});

// ── createApiBaseUrlResolver ─────────────────────────────────
describe("createApiBaseUrlResolver", () => {
    it("uses public base URL when window is defined", () => {
        const resolver = createApiBaseUrlResolver({
            publicApiBaseUrl: "https://public.test",
            internalApiBaseUrl: "https://internal.test"
        });
        expect(resolver.publicApiBaseUrl).toBe("https://public.test");
        expect(resolver.internalApiBaseUrl).toBe("https://internal.test");
    });

    it("provides default base URL", () => {
        const resolver = createApiBaseUrlResolver();
        expect(resolver.publicApiBaseUrl).toBe("https://localhost:5001");
    });
});

// ── createApiRequest (auth refresh retry) ────────────────────
describe("createApiRequest", () => {
    let fetchMock: ReturnType<typeof vi.fn>;
    let refreshSession: ReturnType<typeof vi.fn>;
    let notifyUnauthorized: ReturnType<typeof vi.fn>;

    function createRequest(overrides: Record<string, unknown> = {}) {
        return createApiRequest({
            resolveApiBaseUrl: () => "https://api.test",
            getAccessToken: () => "tok",
            refreshSession,
            notifyUnauthorized,
            shouldAttachAccessToken: () => true,
            shouldAttemptRefresh: () => true,
            getErrorMessage: (p) => (p as Record<string, string>)?.message ?? "",
            createError: (msg, status, payload) => new ApiError(msg, status, payload),
            ...overrides
        });
    }

    beforeEach(() => {
        fetchMock = vi.fn();
        vi.stubGlobal("fetch", fetchMock);
        refreshSession = vi.fn();
        notifyUnauthorized = vi.fn();
    });

    it("returns parsed JSON on success", async () => {
        fetchMock.mockResolvedValue(mockJsonResponse(200, { id: 1 }));
        const req = createRequest();
        const result = await req("/items");
        expect(result).toEqual({ id: 1 });
        expect(fetchMock).toHaveBeenCalledTimes(1);
    });

    it("returns undefined for 204", async () => {
        fetchMock.mockResolvedValue(mockJsonResponse(204, undefined));
        const req = createRequest();
        const result = await req("/items");
        expect(result).toBeUndefined();
    });

    it("retries once after successful refresh on 401", async () => {
        fetchMock
            .mockResolvedValueOnce(mockJsonResponse(401, { message: "expired" }))
            .mockResolvedValueOnce(mockJsonResponse(200, { data: "ok" }));
        refreshSession.mockResolvedValue(true);

        const req = createRequest();
        const result = await req("/protected");

        expect(result).toEqual({ data: "ok" });
        expect(fetchMock).toHaveBeenCalledTimes(2);
        expect(refreshSession).toHaveBeenCalledTimes(1);
        expect(notifyUnauthorized).not.toHaveBeenCalled();
    });

    it("notifies unauthorized when refresh fails on 401", async () => {
        fetchMock.mockResolvedValue(mockJsonResponse(401, { message: "expired" }));
        refreshSession.mockResolvedValue(false);

        const req = createRequest();
        await expect(req("/protected")).rejects.toThrow(ApiError);
        expect(refreshSession).toHaveBeenCalledTimes(1);
        expect(notifyUnauthorized).toHaveBeenCalledTimes(1);
    });

    it("notifies unauthorized when no refresh capability on 401", async () => {
        fetchMock.mockResolvedValue(mockJsonResponse(401, { message: "expired" }));
        const req = createRequest({ refreshSession: undefined });

        await expect(req("/protected")).rejects.toThrow(ApiError);
        expect(fetchMock).toHaveBeenCalledTimes(1);
        expect(notifyUnauthorized).toHaveBeenCalledTimes(1);
    });

    it("deduplicates concurrent 401s (single refresh)", async () => {
        const freshTokenResponse = mockJsonResponse(200, { data: "fresh" });
        fetchMock
            .mockResolvedValueOnce(mockJsonResponse(401, { message: "expired" }))
            .mockResolvedValueOnce(mockJsonResponse(401, { message: "expired" }))
            .mockResolvedValueOnce(freshTokenResponse)
            .mockResolvedValueOnce(freshTokenResponse);
        refreshSession.mockResolvedValue(true);

        const req = createRequest();
        const [r1, r2] = await Promise.all([req("/a"), req("/b")]);

        expect(r1).toEqual({ data: "fresh" });
        expect(r2).toEqual({ data: "fresh" });
        expect(refreshSession).toHaveBeenCalledTimes(1);
        expect(fetchMock).toHaveBeenCalledTimes(4);
    });

    it("throws ApiError on non-401 error status", async () => {
        fetchMock.mockResolvedValue(mockJsonResponse(403, { message: "forbidden" }));
        const req = createRequest();

        try {
            await req("/admin");
            expect.unreachable();
        } catch (err) {
            expect(err).toBeInstanceOf(ApiError);
            expect((err as ApiError).status).toBe(403);
            expect((err as ApiError).message).toBe("forbidden");
        }
    });

    it("does not attempt refresh when policy says no", async () => {
        fetchMock.mockResolvedValue(mockJsonResponse(401, { message: "expired" }));
        const req = createRequest({ shouldAttemptRefresh: () => false });

        await expect(req("/login")).rejects.toThrow(ApiError);
        expect(refreshSession).not.toHaveBeenCalled();
        expect(notifyUnauthorized).toHaveBeenCalledTimes(1);
    });
});

// ── createPublicApiRequest ───────────────────────────────────
describe("createPublicApiRequest", () => {
    it("does not attempt refresh on 401", async () => {
        const fetchMock = vi.fn().mockResolvedValue(mockJsonResponse(401, { message: "unauthorized" }));
        vi.stubGlobal("fetch", fetchMock);

        const req = createPublicApiRequest({
            resolveApiBaseUrl: () => "https://api.test",
            getErrorMessage: (p) => (p as Record<string, string>)?.message ?? "",
            createError: (msg, status, payload) => new ApiError(msg, status, payload)
        });

        await expect(req("/public")).rejects.toThrow(ApiError);
        expect(fetchMock).toHaveBeenCalledTimes(1);
    });
});


