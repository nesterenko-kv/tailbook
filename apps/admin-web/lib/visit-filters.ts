export type VisitFiltersState = {
    status: string;
    groomerId: string;
    from: string;
    to: string;
    appointmentId: string;
};

export function buildVisitFilterQuery(filters: VisitFiltersState, page: number = 1, sortBy?: string, sortDirection?: string): string {
    const query = new URLSearchParams({ page: String(page), pageSize: "25" });
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
    if (sortBy) query.set("sortBy", sortBy);
    if (sortDirection) query.set("sortDirection", sortDirection);
    return `?${query.toString()}`;
}
