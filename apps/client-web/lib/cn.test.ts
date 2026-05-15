import { describe, it, expect } from "vitest";
import { cn } from "./cn";

describe("cn", () => {
    it("joins truthy values with space", () => {
        expect(cn("a", "b", "c")).toBe("a b c");
    });

    it("filters out false", () => {
        expect(cn("a", false, "c")).toBe("a c");
    });

    it("filters out null", () => {
        expect(cn("a", null, "c")).toBe("a c");
    });

    it("filters out undefined", () => {
        expect(cn("a", undefined, "c")).toBe("a c");
    });

    it("returns empty string for no truthy values", () => {
        expect(cn(false, null, undefined)).toBe("");
    });

    it("returns empty string for no arguments", () => {
        expect(cn()).toBe("");
    });

    it("handles single argument", () => {
        expect(cn("foo")).toBe("foo");
    });
});
