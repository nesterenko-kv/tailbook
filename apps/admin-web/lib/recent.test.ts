import { describe, it, expect, beforeEach } from "vitest";
import { getRecentPetIds, addRecentPetId, getRecentVisitIds, addRecentVisitId } from "./recent";

describe("recent IDs", () => {
    beforeEach(() => {
        window.localStorage.clear();
    });

    describe("pet IDs", () => {
        it("returns empty array initially", () => {
            expect(getRecentPetIds()).toEqual([]);
        });

        it("returns added ID", () => {
            addRecentPetId("pet-1");
            expect(getRecentPetIds()).toContain("pet-1");
        });

        it("moves existing ID to front on re-add", () => {
            addRecentPetId("pet-1");
            addRecentPetId("pet-2");
            addRecentPetId("pet-1");
            const ids = getRecentPetIds();
            expect(ids[0]).toBe("pet-1");
            expect(ids).toHaveLength(2);
        });

        it("limits to 10 items", () => {
            for (let i = 0; i < 15; i++) {
                addRecentPetId(`pet-${i}`);
            }
            const ids = getRecentPetIds();
            expect(ids).toHaveLength(10);
            expect(ids[0]).toBe("pet-14");
        });
    });

    describe("visit IDs", () => {
        it("returns empty array initially", () => {
            expect(getRecentVisitIds()).toEqual([]);
        });

        it("stores and retrieves visit IDs separately from pet IDs", () => {
            addRecentPetId("pet-1");
            addRecentVisitId("visit-1");
            expect(getRecentPetIds()).toContain("pet-1");
            expect(getRecentVisitIds()).toContain("visit-1");
            expect(getRecentPetIds()).not.toContain("visit-1");
        });
    });
});
