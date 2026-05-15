import { describe, it, expect } from "vitest";
import { formatDateTime, formatDate, formatMoney } from "./format";

describe("formatDateTime", () => {
    it("returns fallback for null", () => {
        expect(formatDateTime(null)).toBe("—");
    });

    it("returns fallback for undefined", () => {
        expect(formatDateTime(undefined)).toBe("—");
    });

    it("returns fallback for empty string", () => {
        expect(formatDateTime("")).toBe("—");
    });

    it("returns fallback for invalid date", () => {
        expect(formatDateTime("not-a-date")).toBe("—");
    });

    it("formats a valid date string", () => {
        const result = formatDateTime("2026-05-14T10:30:00Z");
        expect(result).toContain("2026");
        expect(result).toContain("14");
        expect(result).toContain("05");
    });

    it("accepts custom fallback", () => {
        expect(formatDateTime(null, "N/A")).toBe("N/A");
    });
});

describe("formatDate", () => {
    it("returns fallback for null", () => {
        expect(formatDate(null)).toBe("—");
    });

    it("returns fallback for empty string", () => {
        expect(formatDate("")).toBe("—");
    });

    it("formats a valid date string", () => {
        const result = formatDate("2026-05-14T10:30:00Z");
        expect(result).toContain("2026");
        expect(result).toContain("14");
        expect(result).toContain("05");
    });
});

describe("formatMoney", () => {
    it("returns fallback for null", () => {
        expect(formatMoney(null)).toBe("—");
    });

    it("returns fallback for undefined", () => {
        expect(formatMoney(undefined)).toBe("—");
    });

    it("formats a valid amount with default currency", () => {
        const result = formatMoney(1500);
        expect(result).toContain("1");
        expect(result).toContain("500");
    });

    it("formats a valid amount with specified currency", () => {
        const result = formatMoney(99.99, "USD");
        expect(result).toContain("99");
        expect(result).toContain("99");
    });

    it("handles zero", () => {
        const result = formatMoney(0);
        expect(result).toBeTruthy();
    });
});
