export type VisitFiltersState = {
    status: string;
    groomerId: string;
    from: string;
    to: string;
    appointmentId: string;
};

export function buildVisitFilterQuery(filters: VisitFiltersState): string {
    const query = new URLSearchParams({ page: "1", pageSize: "25" });
    if (filters.status) query.set("status", filters.status);
    if (filters.groomerId) query.set("groomerId", filters.groomerId);
    if (filters.appointmentId.trim()) query.set("appointmentId", filters.appointmentId.trim());
    if (filters.from) {
        try {
            query.set("from", new Date(filters.from).toISOString());
        } catch {
            // ignore invalid date
        }
    }
    if (filters.to) {
        try {
            query.set("to", new Date(filters.to).toISOString());
        } catch {
            // ignore invalid date
        }
    }
    return `?${query.toString()}`;
}
