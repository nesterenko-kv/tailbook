"use client";

import type { ReactNode } from "react";
import { useEffect, useState } from "react";
import { usePathname, useRouter } from "next/navigation";
import { getAdminToken } from "@/lib/auth";

export function AuthGuard({ children }: { children: ReactNode }) {
  const router = useRouter();
  const pathname = usePathname();
  const [isReady, setIsReady] = useState(false);

  useEffect(() => {
    const token = getAdminToken();
    if (!token && pathname !== "/login") {
      router.replace("/login");
      return;
    }
    setIsReady(true);
  }, [pathname, router]);

  if (!isReady) {
    return <div className="p-8 text-sm text-slate-300">Checking session…</div>;
  }

  return <>{children}</>;
}
