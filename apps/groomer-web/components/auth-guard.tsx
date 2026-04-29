"use client";

import type { ReactNode } from "react";
import { useEffect, useState } from "react";
import { usePathname, useRouter } from "next/navigation";
import { apiRequest } from "@/lib/api";
import { clearSession, getStoredAccessToken, getStoredRefreshToken, GROOMER_UNAUTHORIZED_EVENT, storeProfile } from "@/lib/auth";
import type { IdentityMeResponse } from "@/lib/types";

export function AuthGuard({ children }: { children: ReactNode }) {
    const router = useRouter();
    const pathname = usePathname();
    const [ready, setReady] = useState(false);

    useEffect(() => {
        let cancelled = false;

        function redirectToLogin() {
            clearSession();
            if (pathname !== "/login") {
                router.replace("/login");
            }
        }

        async function verifySession() {
            const token = getStoredAccessToken();
            const refreshToken = getStoredRefreshToken();
            if (!token && !refreshToken) {
                redirectToLogin();
                return;
            }

            try {
                const me = await apiRequest<IdentityMeResponse>("/api/identity/me");
                if (cancelled) {
                    return;
                }

                storeProfile(me.email, me.displayName);
                setReady(true);
            } catch {
                if (!cancelled) {
                    redirectToLogin();
                }
            }
        }

        function handleUnauthorized() {
            if (!cancelled) {
                redirectToLogin();
            }
        }

        window.addEventListener(GROOMER_UNAUTHORIZED_EVENT, handleUnauthorized);
        void verifySession();

        return () => {
            cancelled = true;
            window.removeEventListener(GROOMER_UNAUTHORIZED_EVENT, handleUnauthorized);
        };
    }, [pathname, router]);

    if (!ready) {
        return <div className="p-6 text-sm text-slate-400">Checking groomer session…</div>;
    }

    return <>{children}</>;
}
