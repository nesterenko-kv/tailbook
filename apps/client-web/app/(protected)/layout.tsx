import type { ReactNode } from "react";
import { AuthGuard } from "@/components/auth-guard";

export default function ProtectedLayout({ children }: Readonly<{ children: ReactNode }>) {
  return <AuthGuard>{children}</AuthGuard>;
}
