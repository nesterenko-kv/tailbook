import { describe, it, expect } from "vitest";
import { unwrapItems } from "./contracts";

describe("unwrapItems", () => {
    it("returns array when given an array", () => {
        const result = unwrapItems([{ id: 1 }, { id: 2 }]);
        expect(result).toHaveLength(2);
    });

    it("returns items from envelope", () => {
        const value = { items: [{ id: 1 }, { id: 2 }], page: 1, pageSize: 25, totalCount: 2 };
        const result = unwrapItems(value as unknown as { id: number }[]);
        expect(result).toHaveLength(2);
    });

    it("returns empty array for null", () => {
        const result = unwrapItems(null);
        expect(result).toEqual([]);
    });

    it("returns empty array for undefined", () => {
        const result = unwrapItems(undefined);
        expect(result).toEqual([]);
    });

    it("handles empty array", () => {
        const result = unwrapItems([]);
        expect(result).toEqual([]);
    });

    it("handles envelope with empty items", () => {
        const value = { items: [] as never[], page: 1, pageSize: 25, totalCount: 0 };
        const result = unwrapItems(value as unknown as never[]);
        expect(result).toEqual([]);
    });

    it("returns empty array for object without items", () => {
        const result = unwrapItems({} as unknown as null);
        expect(result).toEqual([]);
    });
});
