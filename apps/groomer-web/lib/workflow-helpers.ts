export type AppointmentQueueRange = "today" | "upcoming" | "all";

export type AppointmentQueueItem = {
    id: string;
    startAt: string;
    endAt: string;
    status: string;
};

export type AppointmentQueueFilter = {
    range: AppointmentQueueRange;
    status?: string | null;
    now?: Date;
};

export type AppointmentStatusTone = "neutral" | "active" | "complete" | "blocked";

export type AppointmentStatusDisplay = {
    label: string;
    tone: AppointmentStatusTone;
};

export type VisitChecklistItem = {
    expectedComponents: Array<{
        id: string;
        procedureId: string;
    }>;
    performedProcedures: Array<{
        procedureId: string;
    }>;
    skippedComponents: Array<{
        offerVersionComponentId: string;
    }>;
};

export type VisitChecklistSummary = {
    expectedCount: number;
    performedCount: number;
    skippedCount: number;
    remainingCount: number;
    isReadyForAdminHandoff: boolean;
};

export const SKIP_REASON_OPTIONS = [
    { code: "OPERATIONAL_DECISION", label: "Operational decision" },
    { code: "PET_STRESS", label: "Pet stress" },
    { code: "SAFETY_CONCERN", label: "Safety concern" },
    { code: "OWNER_REQUEST", label: "Owner request" }
] as const;

export function normalizeStatus(status: string) {
    return status.trim().toLowerCase();
}

export function getAppointmentStatusDisplay(status: string): AppointmentStatusDisplay {
    switch (normalizeStatus(status).replace(/[\s_-]/g, "")) {
        case "checkedin":
            return { label: "Checked in", tone: "active" };
        case "inprogress":
            return { label: "In progress", tone: "active" };
        case "completed":
        case "closed":
            return { label: "Completed", tone: "complete" };
        case "cancelled":
        case "canceled":
            return { label: "Cancelled", tone: "blocked" };
        case "noshow":
            return { label: "No show", tone: "blocked" };
        case "confirmed":
            return { label: "Confirmed", tone: "neutral" };
        case "scheduled":
            return { label: "Scheduled", tone: "neutral" };
        default:
            return { label: humanizeStatus(status), tone: "neutral" };
    }
}

export function filterAppointmentQueue<TItem extends AppointmentQueueItem>(items: TItem[], filter: AppointmentQueueFilter) {
    const now = filter.now ?? new Date();
    const status = normalizeStatus(filter.status ?? "all");

    return items
        .filter((item) => isInSelectedRange(item, filter.range, now))
        .filter((item) => status === "all" || normalizeStatus(item.status) === status)
        .slice()
        .sort((left, right) => Date.parse(left.startAt) - Date.parse(right.startAt));
}

export function getVisitChecklistSummary(items: VisitChecklistItem[]): VisitChecklistSummary {
    let expectedCount = 0;
    let performedCount = 0;
    let skippedCount = 0;
    let remainingCount = 0;

    for (const item of items) {
        const performedProcedureIds = new Set(item.performedProcedures.map((entry) => entry.procedureId));
        const skippedComponentIds = new Set(item.skippedComponents.map((entry) => entry.offerVersionComponentId));

        for (const component of item.expectedComponents) {
            expectedCount += 1;
            const isSkipped = skippedComponentIds.has(component.id);
            const isPerformed = performedProcedureIds.has(component.procedureId);

            if (isSkipped) {
                skippedCount += 1;
            } else if (isPerformed) {
                performedCount += 1;
            } else {
                remainingCount += 1;
            }
        }
    }

    return {
        expectedCount,
        performedCount,
        skippedCount,
        remainingCount,
        isReadyForAdminHandoff: expectedCount > 0 && remainingCount === 0
    };
}

export function getSkipReasonLabel(code: string) {
    return SKIP_REASON_OPTIONS.find((option) => option.code === code)?.label ?? humanizeStatus(code);
}

export function createActionGuard() {
    let busy = false;

    return {
        tryAcquire() {
            if (busy) {
                return false;
            }

            busy = true;
            return true;
        },
        release() {
            busy = false;
        },
        get busy() {
            return busy;
        }
    };
}

function isInSelectedRange(item: AppointmentQueueItem, range: AppointmentQueueRange, now: Date) {
    if (range === "all") {
        return true;
    }

    const start = new Date(item.startAt);
    if (Number.isNaN(start.getTime())) {
        return false;
    }

    const todayStart = startOfLocalDay(now);
    const tomorrowStart = addDays(todayStart, 1);

    if (range === "today") {
        return start >= todayStart && start < tomorrowStart;
    }

    return start >= tomorrowStart;
}

function startOfLocalDay(value: Date) {
    return new Date(value.getFullYear(), value.getMonth(), value.getDate());
}

function addDays(value: Date, days: number) {
    const copy = new Date(value);
    copy.setDate(copy.getDate() + days);
    return copy;
}

function humanizeStatus(status: string) {
    const normalized = status.trim().replace(/[_-]+/g, " ");
    if (!normalized) {
        return "Unknown";
    }

    return normalized
        .split(/\s+/)
        .map((word) => word.charAt(0).toUpperCase() + word.slice(1).toLowerCase())
        .join(" ");
}
