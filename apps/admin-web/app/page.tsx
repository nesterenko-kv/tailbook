"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { getAdminToken } from "@/lib/auth";

export default function HomePage() {
  const router = useRouter();

  useEffect(() => {
    router.replace(getAdminToken() ? "/clients" : "/login");
  }, [router]);

  return <div className="p-8 text-sm text-slate-300">Redirecting…</div>;
}
