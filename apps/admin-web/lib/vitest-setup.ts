import { vi } from "vitest";

const store: Record<string, string> = {};

vi.stubGlobal("window", {
    localStorage: {
        getItem: (key: string) => store[key] ?? null,
        setItem: (key: string, value: string) => { store[key] = value; },
        removeItem: (key: string) => { delete store[key]; },
        clear: () => { Object.keys(store).forEach((key) => delete store[key]); }
    },
    dispatchEvent: vi.fn(),
    CustomEvent: vi.fn()
});
