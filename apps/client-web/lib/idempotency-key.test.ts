import { describe, it, expect } from "vitest";
import { generateIdempotencyKey } from "./idempotency-key";

describe("generateIdempotencyKey", () => {
    it("returns a string", () => {
        const key = generateIdempotencyKey();
        expect(typeof key).toBe("string");
    });

    it("returns a UUID-formatted string", () => {
        const key = generateIdempotencyKey();
        expect(key).toMatch(/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i);
    });

    it("returns unique values on successive calls", () => {
        const key1 = generateIdempotencyKey();
        const key2 = generateIdempotencyKey();
        expect(key1).not.toBe(key2);
    });
});