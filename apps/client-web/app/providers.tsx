"use client";

import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { useState, type ReactNode } from "react";
import { ClientBookingProvider } from "@/lib/client-booking-context";

export function Providers({ children }: Readonly<{ children?: ReactNode }>) {
  const [queryClient] = useState(() => new QueryClient());

  return (
    <QueryClientProvider client={queryClient}>
      <ClientBookingProvider>{children}</ClientBookingProvider>
    </QueryClientProvider>
  );
}
