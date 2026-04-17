"use client";

import { useEffect, useState } from "react";
import { usePathname, useRouter } from "next/navigation";
import { getStoredAccessToken } from "@/lib/auth";

export function AuthGuard({ children }: Readonly<{ children: React.ReactNode }>) {
    const router = useRouter();
    const pathname = usePathname();
    const [ready, setReady] = useState(false);

    useEffect(() => {
        const token = getStoredAccessToken();
        if (!token && pathname !== "/login" && pathname !== "/register") {
            router.replace("/login");
            return;
        }

        setReady(true);
    }, [pathname, router]);

    if (!ready) {
        return <div className="p-6 text-sm text-slate-400">Loading portal…</div>;
    }

    return <>{children}</>;
}
