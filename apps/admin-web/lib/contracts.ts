import type { ItemsEnvelope } from "@/lib/types";

export function unwrapItems<T>(value: T[] | ItemsEnvelope<T> | null | undefined): T[] {
    if (Array.isArray(value)) {
        return value;
    }

    if (value && Array.isArray(value.items)) {
        return value.items;
    }

    return [];
}
