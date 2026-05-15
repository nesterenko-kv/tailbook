import { describe, it, expect, afterEach } from "vitest";
import { resolveApiBaseUrl } from "./env";

describe("env", () => {
    const originalEnv = { ...process.env };

    afterEach(() => {
        process.env = { ...originalEnv };
    });

    it("resolveApiBaseUrl is defined", () => {
        expect(resolveApiBaseUrl).toBeDefined();
    });

    it("returns string from resolveApiBaseUrl", () => {
        const url = resolveApiBaseUrl();
        expect(typeof url).toBe("string");
        expect(url.length).toBeGreaterThan(0);
    });
});
