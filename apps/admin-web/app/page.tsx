"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";

export default function HomePage() {
  const router = useRouter();

  useEffect(() => {
    router.replace("/clients");
  }, [router]);

  return <div className="p-8 text-sm text-slate-300">Redirecting…</div>;
}
