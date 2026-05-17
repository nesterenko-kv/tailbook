import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { AuthGuard } from "./auth-guard";
import * as auth from "@/lib/auth";

const mockReplace = vi.fn();
let mockPathname = "/clients";

vi.mock("next/navigation", () => ({
    useRouter: () => ({ replace: mockReplace }),
    usePathname: () => mockPathname
}));

const mockApiRequest = vi.fn();
vi.mock("@/lib/api", () => ({
    apiRequest: (...args: unknown[]) => mockApiRequest(...args)
}));

const mockSetAdminProfile = vi.spyOn(auth, "setAdminProfile").mockImplementation(() => {});
const mockClearAdminSession = vi.spyOn(auth, "clearAdminSession").mockImplementation(() => {});

const mePayload = { email: "admin@test.com", displayName: "Admin User", userId: "u1", subjectId: "s1", roles: ["admin"], permissions: ["*"] };

describe("AuthGuard", () => {
    beforeEach(() => {
        vi.clearAllMocks();
        mockPathname = "/clients";
    });

    it("shows loading state initially", () => {
        mockApiRequest.mockReturnValue(new Promise(() => {}));
        render(<AuthGuard><div>Protected</div></AuthGuard>);
        expect(screen.getByText("Checking session…")).toBeInTheDocument();
        expect(screen.queryByText("Protected")).not.toBeInTheDocument();
    });

    it("renders children on successful verification", async () => {
        mockApiRequest.mockResolvedValue(mePayload);
        render(<AuthGuard><div>Protected</div></AuthGuard>);
        await waitFor(() => expect(screen.getByText("Protected")).toBeInTheDocument());
        expect(screen.queryByText("Checking session…")).not.toBeInTheDocument();
    });

    it("stores profile on successful verification", async () => {
        mockApiRequest.mockResolvedValue(mePayload);
        render(<AuthGuard><div>Protected</div></AuthGuard>);
        await waitFor(() => expect(mockSetAdminProfile).toHaveBeenCalledWith({ email: "admin@test.com", displayName: "Admin User" }));
    });

    it("redirects to login on API error", async () => {
        mockApiRequest.mockRejectedValue(new Error("Unauthorized"));
        render(<AuthGuard><div>Protected</div></AuthGuard>);
        await waitFor(() => expect(mockClearAdminSession).toHaveBeenCalled());
        await waitFor(() => expect(mockReplace).toHaveBeenCalledWith("/login"));
    });

    it("does not redirect when already on login page", async () => {
        mockPathname = "/login";
        mockApiRequest.mockRejectedValue(new Error("Unauthorized"));
        render(<AuthGuard><div>Protected</div></AuthGuard>);
        await waitFor(() => expect(mockClearAdminSession).toHaveBeenCalled());
        expect(mockReplace).not.toHaveBeenCalled();
    });

    it("redirects to login on unauthorized event", async () => {
        mockApiRequest.mockResolvedValue(mePayload);
        render(<AuthGuard><div>Protected</div></AuthGuard>);
        await waitFor(() => expect(screen.getByText("Protected")).toBeInTheDocument());
        window.dispatchEvent(new CustomEvent(auth.ADMIN_UNAUTHORIZED_EVENT));
        await waitFor(() => expect(mockClearAdminSession).toHaveBeenCalled());
        await waitFor(() => expect(mockReplace).toHaveBeenCalledWith("/login"));
    });
});
