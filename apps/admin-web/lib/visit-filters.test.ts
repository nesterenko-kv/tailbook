import { describe, it, expect } from "vitest";
import { buildVisitFilterQuery } from "./visit-filters";

describe("buildVisitFilterQuery", () => {
    it("includes page and pageSize by default", () => {
        const q = buildVisitFilterQuery({ status: "", groomerId: "", from: "", to: "", appointmentId: "" });
        expect(q).toContain("page=1");
        expect(q).toContain("pageSize=25");
    });

    it("adds status filter when set", () => {
        const q = buildVisitFilterQuery({ status: "Open", groomerId: "", from: "", to: "", appointmentId: "" });
        expect(q).toContain("status=Open");
    });

    it("omits status filter when empty", () => {
        const q = buildVisitFilterQuery({ status: "", groomerId: "", from: "", to: "", appointmentId: "" });
        expect(q).not.toContain("status=");
    });

    it("adds groomerId filter when set", () => {
        const q = buildVisitFilterQuery({ status: "", groomerId: "g-1", from: "", to: "", appointmentId: "" });
        expect(q).toContain("groomerId=g-1");
    });

    it("adds appointmentId filter when trimmed value is non-empty", () => {
        const q = buildVisitFilterQuery({ status: "", groomerId: "", from: "", to: "", appointmentId: "  123  " });
        expect(q).toContain("appointmentId=123");
    });

    it("omits appointmentId when blank", () => {
        const q = buildVisitFilterQuery({ status: "", groomerId: "", from: "", to: "", appointmentId: "   " });
        expect(q).not.toContain("appointmentId=");
    });

    it("converts from date to ISO string", () => {
        const q = buildVisitFilterQuery({ status: "", groomerId: "", from: "2026-05-14T10:00", to: "", appointmentId: "" });
        expect(q).toContain("from=");
        expect(decodeURIComponent(q)).toContain("2026-05-14");
    });

    it("converts to date to ISO string", () => {
        const q = buildVisitFilterQuery({ status: "", groomerId: "", from: "", to: "2026-05-15T18:00", appointmentId: "" });
        expect(q).toContain("to=");
        expect(decodeURIComponent(q)).toContain("2026-05-15");
    });

    it("produces a complete query string", () => {
        const q = buildVisitFilterQuery({ status: "Closed", groomerId: "g-42", from: "", to: "", appointmentId: "abc" });
        const params = new URLSearchParams(q.startsWith("?") ? q.slice(1) : q);
        expect(params.get("status")).toBe("Closed");
        expect(params.get("groomerId")).toBe("g-42");
        expect(params.get("appointmentId")).toBe("abc");
        expect(params.get("page")).toBe("1");
        expect(params.get("pageSize")).toBe("25");
    });
});
