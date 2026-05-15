import { describe, it, expect, beforeEach } from "vitest";
import {
    getAdminToken,
    getAdminRefreshToken,
    setAdminSession,
    clearAdminSession,
    setAdminProfile,
    notifyAdminUnauthorized,
    getAdminEmail,
    getAdminDisplayName
} from "./auth";

describe("admin auth session management", () => {
    beforeEach(() => {
        clearAdminSession();
    });

    describe("initial state", () => {
        it("returns null access token initially", () => {
            expect(getAdminToken()).toBeNull();
        });

        it("returns null refresh token initially", () => {
            expect(getAdminRefreshToken()).toBeNull();
        });
    });

    describe("setAdminSession", () => {
        it("stores access token", () => {
            setAdminSession({ accessToken: "token-123", email: "admin@test.com", displayName: "Admin" });
            expect(getAdminToken()).toBe("token-123");
        });

        it("stores refresh token when provided", () => {
            setAdminSession({ accessToken: "token-123", email: "admin@test.com", displayName: "Admin", refreshToken: "refresh-abc" });
            expect(getAdminRefreshToken()).toBe("refresh-abc");
        });

        it("stores null refresh token when not provided", () => {
            setAdminSession({ accessToken: "token-123", email: "admin@test.com", displayName: "Admin" });
            expect(getAdminRefreshToken()).toBeNull();
        });

        it("replaces old tokens on new session", () => {
            setAdminSession({ accessToken: "old-token", email: "old@test.com", displayName: "Old", refreshToken: "old-refresh" });
            setAdminSession({ accessToken: "new-token", email: "new@test.com", displayName: "New", refreshToken: "new-refresh" });
            expect(getAdminToken()).toBe("new-token");
            expect(getAdminRefreshToken()).toBe("new-refresh");
        });
    });

    describe("clearAdminSession", () => {
        it("clears access and refresh tokens", () => {
            setAdminSession({ accessToken: "token-123", email: "admin@test.com", displayName: "Admin", refreshToken: "refresh-abc" });
            clearAdminSession();
            expect(getAdminToken()).toBeNull();
            expect(getAdminRefreshToken()).toBeNull();
        });

        it("is safe to call when already empty", () => {
            clearAdminSession();
            expect(getAdminToken()).toBeNull();
        });
    });

    describe("setAdminProfile", () => {
        it("stores email and display name", () => {
            setAdminProfile({ email: "admin@test.com", displayName: "Admin User" });
            expect(getAdminEmail()).toBe("admin@test.com");
            expect(getAdminDisplayName()).toBe("Admin User");
        });

        it("overwrites previous profile", () => {
            setAdminProfile({ email: "old@test.com", displayName: "Old" });
            setAdminProfile({ email: "new@test.com", displayName: "New" });
            expect(getAdminEmail()).toBe("new@test.com");
            expect(getAdminDisplayName()).toBe("New");
        });
    });

    describe("notifyAdminUnauthorized", () => {
        it("clears tokens on unauthorized", () => {
            setAdminSession({ accessToken: "token-123", email: "admin@test.com", displayName: "Admin", refreshToken: "refresh-abc" });
            notifyAdminUnauthorized();
            expect(getAdminToken()).toBeNull();
            expect(getAdminRefreshToken()).toBeNull();
        });

        it("is safe to call when already empty", () => {
            notifyAdminUnauthorized();
            expect(getAdminToken()).toBeNull();
        });
    });
});
