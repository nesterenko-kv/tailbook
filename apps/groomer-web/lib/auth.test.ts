import { describe, it, expect, beforeEach } from "vitest";
import {
    getStoredAccessToken,
    getStoredRefreshToken,
    storeSession,
    clearSession,
    storeProfile,
    notifyUnauthorized,
    getStoredEmail,
    getStoredDisplayName
} from "./auth";

describe("groomer auth session management", () => {
    beforeEach(() => {
        clearSession();
    });

    describe("initial state", () => {
        it("returns null access token initially", () => {
            expect(getStoredAccessToken()).toBeNull();
        });

        it("returns null refresh token initially", () => {
            expect(getStoredRefreshToken()).toBeNull();
        });
    });

    describe("storeSession", () => {
        it("stores access token", () => {
            storeSession("token-123", "test@test.com", "Test User");
            expect(getStoredAccessToken()).toBe("token-123");
        });

        it("stores refresh token when provided", () => {
            storeSession("token-123", "test@test.com", "Test User", "refresh-abc");
            expect(getStoredRefreshToken()).toBe("refresh-abc");
        });

        it("stores null refresh token when not provided", () => {
            storeSession("token-123", "test@test.com");
            expect(getStoredRefreshToken()).toBeNull();
        });

        it("replaces old tokens on new session", () => {
            storeSession("old-token", "old@test.com", "Old", "old-refresh");
            storeSession("new-token", "new@test.com", "New", "new-refresh");
            expect(getStoredAccessToken()).toBe("new-token");
            expect(getStoredRefreshToken()).toBe("new-refresh");
        });
    });

    describe("clearSession", () => {
        it("clears access and refresh tokens", () => {
            storeSession("token-123", "test@test.com", "Test User", "refresh-abc");
            clearSession();
            expect(getStoredAccessToken()).toBeNull();
            expect(getStoredRefreshToken()).toBeNull();
        });

        it("is safe to call when already empty", () => {
            clearSession();
            expect(getStoredAccessToken()).toBeNull();
        });
    });

    describe("storeProfile", () => {
        it("stores email and display name", () => {
            storeProfile("test@test.com", "Test User");
            expect(getStoredEmail()).toBe("test@test.com");
            expect(getStoredDisplayName()).toBe("Test User");
        });

        it("overwrites previous profile", () => {
            storeProfile("old@test.com", "Old");
            storeProfile("new@test.com", "New");
            expect(getStoredEmail()).toBe("new@test.com");
            expect(getStoredDisplayName()).toBe("New");
        });
    });

    describe("notifyUnauthorized", () => {
        it("clears tokens on unauthorized", () => {
            storeSession("token-123", "test@test.com", "Test User", "refresh-abc");
            notifyUnauthorized();
            expect(getStoredAccessToken()).toBeNull();
            expect(getStoredRefreshToken()).toBeNull();
        });

        it("is safe to call when already empty", () => {
            notifyUnauthorized();
            expect(getStoredAccessToken()).toBeNull();
        });
    });
});
