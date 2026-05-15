import type { ReactNode } from "react";

export function ClientShell({ children }: Readonly<{ children: ReactNode }>) {
  return <>{children}</>;
}
