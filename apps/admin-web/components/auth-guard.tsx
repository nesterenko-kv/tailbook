"use client";

import type { ReactNode } from "react";
import { useEffect, useState } from "react";
import { usePathname, useRouter } from "next/navigation";
import { clearAdminSession, getAdminToken, setAdminProfile, ADMIN_UNAUTHORIZED_EVENT } from "@/lib/auth";
import { apiRequest } from "@/lib/api";
import type { MeResponse } from "@/lib/types";

export function AuthGuard({ children }: { children: ReactNode }) {
  const router = useRouter();
  const pathname = usePathname();
  const [isReady, setIsReady] = useState(false);

  useEffect(() => {
    let cancelled = false;

    function redirectToLogin() {
      clearAdminSession();
      if (pathname !== "/login") {
        router.replace("/login");
      }
    }

    async function verifySession() {
      const token = getAdminToken();
      if (!token) {
        redirectToLogin();
        return;
      }

      try {
        const me = await apiRequest<MeResponse>("/api/identity/me");
        if (cancelled) {
          return;
        }

        setAdminProfile({ email: me.email, displayName: me.displayName });
        setIsReady(true);
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

    window.addEventListener(ADMIN_UNAUTHORIZED_EVENT, handleUnauthorized);
    void verifySession();

    return () => {
      cancelled = true;
      window.removeEventListener(ADMIN_UNAUTHORIZED_EVENT, handleUnauthorized);
    };
  }, [pathname, router]);

  if (!isReady) {
    return <div className="p-8 text-sm text-slate-300">Checking session…</div>;
  }

  return <>{children}</>;
}
