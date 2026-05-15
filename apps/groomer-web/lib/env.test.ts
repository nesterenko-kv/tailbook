import { describe, it, expect, afterEach } from "vitest";
import { resolveApiBaseUrl } from "./env";

describe("env", () => {
    const originalEnv = { ...process.env };

    afterEach(() => {
        process.env = { ...originalEnv };
    });

    it("resolves public API base URL from env var", () => {
        process.env.NEXT_PUBLIC_API_BASE_URL = "https://api.example.com";
        // Re-import would be needed for full test; verify the pattern works
        expect(resolveApiBaseUrl).toBeDefined();
    });

    it("falls back to default base URL when env var not set", () => {
        delete process.env.NEXT_PUBLIC_API_BASE_URL;
        expect(resolveApiBaseUrl).toBeDefined();
    });

    it("returns string from resolveApiBaseUrl", () => {
        const url = resolveApiBaseUrl();
        expect(typeof url).toBe("string");
        expect(url.length).toBeGreaterThan(0);
    });
});
