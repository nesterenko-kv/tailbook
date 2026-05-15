import { describe, it, expect } from "vitest";
import { createSubmitGuard } from "./submit-guard";

describe("createSubmitGuard", () => {
    it("allows first invocation", () => {
        const guard = createSubmitGuard();
        expect(guard.tryAcquire()).toBe(true);
        expect(guard.busy).toBe(true);
    });

    it("blocks second concurrent invocation", () => {
        const guard = createSubmitGuard();
        expect(guard.tryAcquire()).toBe(true);
        expect(guard.tryAcquire()).toBe(false);
    });

    it("allows invocation after release", () => {
        const guard = createSubmitGuard();
        guard.tryAcquire();
        guard.release();
        expect(guard.tryAcquire()).toBe(true);
    });

    it("releases in finally block pattern", () => {
        const guard = createSubmitGuard();
        guard.tryAcquire();
        try {
            // simulate async work
        } finally {
            guard.release();
        }
        expect(guard.tryAcquire()).toBe(true);
    });

    it("prevents rapid double-trigger in sync context", () => {
        const guard = createSubmitGuard();
        const results = [guard.tryAcquire(), guard.tryAcquire(), guard.tryAcquire()];
        expect(results).toEqual([true, false, false]);
    });
});
