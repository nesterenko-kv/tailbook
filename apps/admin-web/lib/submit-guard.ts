export function createSubmitGuard() {
    let inFlight = false;
    let busy = false;

    function tryAcquire() {
        if (busy || inFlight) return false;
        inFlight = true;
        busy = true;
        return true;
    }

    function release() {
        inFlight = false;
        busy = false;
    }

    return { tryAcquire, release, get busy() { return busy; } };
}

export type SubmitGuard = ReturnType<typeof createSubmitGuard>;
