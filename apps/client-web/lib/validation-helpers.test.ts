import { describe, it, expect } from "vitest";
import { validateInput, getFirstFieldError } from "./validation-helpers";

describe("validateInput", () => {
    it("returns error for required empty value", () => {
        const result = validateInput({ value: "", required: true, label: "ім'я" });
        expect(result).not.toBeNull();
        expect(result!.message).toContain("ім'я");
    });

    it("returns null for required non-empty value", () => {
        const result = validateInput({ value: "Анна", required: true, label: "ім'я" });
        expect(result).toBeNull();
    });

    it("returns error for value below minLength", () => {
        const result = validateInput({ value: "ab", minLength: 3, label: "пароль" });
        expect(result).not.toBeNull();
        expect(result!.message).toContain("пароль");
    });

    it("returns error for value above maxLength", () => {
        const result = validateInput({ value: "a".repeat(101), maxLength: 100, label: "текст" });
        expect(result).not.toBeNull();
    });

    it("returns error for pattern mismatch", () => {
        const result = validateInput({ value: "invalid", pattern: /^\+38\d{10}$/, label: "номер телефону" });
        expect(result).not.toBeNull();
    });

    it("returns null for matching pattern", () => {
        const result = validateInput({ value: "+380501234567", pattern: /^\+38\d{10}$/, label: "номер телефону" });
        expect(result).toBeNull();
    });

    it("returns null when value is undefined and not required", () => {
        const result = validateInput({ value: undefined, label: "текст" });
        expect(result).toBeNull();
    });
});

describe("getFirstFieldError", () => {
    it("returns null for empty errors", () => {
        expect(getFirstFieldError({})).toBeNull();
    });

    it("returns the message from the first error", () => {
        const result = getFirstFieldError({ fullName: { message: "Name error" }, phone: { message: "Phone error" } });
        expect(result).toBe("Name error");
    });
});