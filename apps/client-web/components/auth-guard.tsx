"use client";

import { useEffect, useState, type ReactNode } from "react";
import { usePathname, useRouter } from "next/navigation";
import { apiRequest } from "@/lib/api";
import { clearSession, CLIENT_UNAUTHORIZED_EVENT, getStoredAccessToken, storeEmail } from "@/lib/auth";
import type { ClientMeResponse } from "@/lib/types";

export function AuthGuard({ children }: Readonly<{ children: ReactNode }>) {
  const router = useRouter();
  const pathname = usePathname();
  const [ready, setReady] = useState(false);

  useEffect(() => {
    let cancelled = false;

    function redirectToLogin() {
      clearSession();
      if (pathname !== "/login" && pathname !== "/register") {
        router.replace("/login");
      }
    }

    async function verifySession() {
      const token = getStoredAccessToken();
      if (!token) {
        redirectToLogin();
        return;
      }

      try {
        const me = await apiRequest<ClientMeResponse>("/api/client/me");
        if (cancelled) {
          return;
        }
        storeEmail(me.email);
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

    window.addEventListener(CLIENT_UNAUTHORIZED_EVENT, handleUnauthorized);
    void verifySession();
    return () => {
      cancelled = true;
      window.removeEventListener(CLIENT_UNAUTHORIZED_EVENT, handleUnauthorized);
    };
  }, [pathname, router]);

  if (!ready) {
    return (
      <div className="min-h-screen bg-background">
        <div className="container flex min-h-screen items-center justify-center">
          <div className="rounded-2xl border border-border bg-card px-8 py-6 text-sm text-muted-foreground shadow-soft">Завантажуємо портал…</div>
        </div>
      </div>
    );
  }

  return <>{children}</>;
}
