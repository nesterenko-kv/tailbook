import { describe, expect, it } from "vitest";
import {
    createActionGuard,
    filterAppointmentQueue,
    getAppointmentStatusDisplay,
    getSkipReasonLabel,
    getVisitChecklistSummary
} from "./workflow-helpers";

const now = new Date("2026-05-14T10:00:00");

const appointments = [
    { id: "tomorrow", startAt: "2026-05-15T09:00:00", endAt: "2026-05-15T10:00:00", status: "Scheduled" },
    { id: "today-late", startAt: "2026-05-14T16:00:00", endAt: "2026-05-14T17:00:00", status: "CheckedIn" },
    { id: "yesterday", startAt: "2026-05-13T16:00:00", endAt: "2026-05-13T17:00:00", status: "Completed" },
    { id: "today-early", startAt: "2026-05-14T08:00:00", endAt: "2026-05-14T09:00:00", status: "Scheduled" }
];

describe("filterAppointmentQueue", () => {
    it("returns today's appointments sorted by start time", () => {
        const result = filterAppointmentQueue(appointments, { range: "today", now });

        expect(result.map((item) => item.id)).toEqual(["today-early", "today-late"]);
    });

    it("returns upcoming appointments after today", () => {
        const result = filterAppointmentQueue(appointments, { range: "upcoming", now });

        expect(result.map((item) => item.id)).toEqual(["tomorrow"]);
    });

    it("filters status case-insensitively", () => {
        const result = filterAppointmentQueue(appointments, { range: "all", status: "scheduled", now });

        expect(result.map((item) => item.id)).toEqual(["today-early", "tomorrow"]);
    });

    it("excludes appointments with invalid dates from date ranges", () => {
        const result = filterAppointmentQueue(
            [{ id: "invalid", startAt: "not-a-date", endAt: "2026-05-14T09:00:00", status: "Scheduled" }],
            { range: "today", now }
        );

        expect(result).toEqual([]);
    });
});

describe("getAppointmentStatusDisplay", () => {
    it("maps active statuses", () => {
        expect(getAppointmentStatusDisplay("CheckedIn")).toEqual({ label: "Checked in", tone: "active" });
        expect(getAppointmentStatusDisplay("in_progress")).toEqual({ label: "In progress", tone: "active" });
    });

    it("maps completed and blocked statuses", () => {
        expect(getAppointmentStatusDisplay("Closed").tone).toBe("complete");
        expect(getAppointmentStatusDisplay("NoShow")).toEqual({ label: "No show", tone: "blocked" });
    });

    it("humanizes unknown statuses", () => {
        expect(getAppointmentStatusDisplay("awaiting_finalization")).toEqual({ label: "Awaiting Finalization", tone: "neutral" });
    });
});

describe("getVisitChecklistSummary", () => {
    it("counts performed, skipped, and remaining expected components", () => {
        const result = getVisitChecklistSummary([
            {
                expectedComponents: [
                    { id: "component-1", procedureId: "procedure-1" },
                    { id: "component-2", procedureId: "procedure-2" },
                    { id: "component-3", procedureId: "procedure-3" }
                ],
                performedProcedures: [{ procedureId: "procedure-1" }],
                skippedComponents: [{ offerVersionComponentId: "component-2" }]
            }
        ]);

        expect(result).toEqual({
            expectedCount: 3,
            performedCount: 1,
            skippedCount: 1,
            remainingCount: 1,
            isReadyForAdminHandoff: false
        });
    });

    it("marks checklist ready for admin handoff when all expected components are accounted for", () => {
        const result = getVisitChecklistSummary([
            {
                expectedComponents: [
                    { id: "component-1", procedureId: "procedure-1" },
                    { id: "component-2", procedureId: "procedure-2" }
                ],
                performedProcedures: [{ procedureId: "procedure-1" }],
                skippedComponents: [{ offerVersionComponentId: "component-2" }]
            }
        ]);

        expect(result.remainingCount).toBe(0);
        expect(result.isReadyForAdminHandoff).toBe(true);
    });

    it("does not mark empty visits ready for handoff", () => {
        const result = getVisitChecklistSummary([]);

        expect(result).toEqual({
            expectedCount: 0,
            performedCount: 0,
            skippedCount: 0,
            remainingCount: 0,
            isReadyForAdminHandoff: false
        });
    });
});

describe("skip reason helpers", () => {
    it("returns configured labels", () => {
        expect(getSkipReasonLabel("SAFETY_CONCERN")).toBe("Safety concern");
    });

    it("humanizes unknown codes", () => {
        expect(getSkipReasonLabel("CUSTOM_REASON")).toBe("Custom Reason");
    });
});

describe("createActionGuard", () => {
    it("blocks duplicate actions until released", () => {
        const guard = createActionGuard();

        expect(guard.tryAcquire()).toBe(true);
        expect(guard.busy).toBe(true);
        expect(guard.tryAcquire()).toBe(false);

        guard.release();

        expect(guard.busy).toBe(false);
        expect(guard.tryAcquire()).toBe(true);
    });
});
