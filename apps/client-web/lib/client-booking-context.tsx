"use client";

import { createContext, useCallback, useContext, useEffect, useMemo, useState, type Dispatch, type ReactNode, type SetStateAction } from "react";
import { emptyBookingState, type BookingState } from "@/lib/booking-helpers";

const STORAGE_KEY = "tailbook.client.booking.v2";

type ClientBookingContextValue = {
  booking: BookingState;
  setBooking: Dispatch<SetStateAction<BookingState>>;
  patchBooking: (patch: Partial<BookingState>) => void;
  resetBooking: () => void;
};

const ClientBookingContext = createContext<ClientBookingContextValue | null>(null);

export function ClientBookingProvider({ children }: Readonly<{ children: ReactNode }>) {
  const [booking, setBooking] = useState<BookingState>(emptyBookingState);
  const [hydrated, setHydrated] = useState(false);

  useEffect(() => {
    if (typeof window === "undefined") {
      setHydrated(true);
      return;
    }

    const raw = window.sessionStorage.getItem(STORAGE_KEY);
    if (!raw) {
      setHydrated(true);
      return;
    }

    try {
      const parsed = JSON.parse(raw);
      setBooking(parsed && typeof parsed === "object" ? { ...emptyBookingState, ...parsed } : emptyBookingState);
    } catch {
      window.sessionStorage.removeItem(STORAGE_KEY);
    } finally {
      setHydrated(true);
    }
  }, []);

  useEffect(() => {
    if (!hydrated || typeof window === "undefined") {
      return;
    }

    window.sessionStorage.setItem(STORAGE_KEY, JSON.stringify(booking));
  }, [booking, hydrated]);

  const patchBooking = useCallback((patch: Partial<BookingState>) => {
    setBooking((current) => ({ ...current, ...patch }));
  }, []);

  const resetBooking = useCallback(() => {
    setBooking(emptyBookingState);
    if (typeof window !== "undefined") {
      window.sessionStorage.removeItem(STORAGE_KEY);
    }
  }, []);

  const value = useMemo(() => ({ booking, setBooking, patchBooking, resetBooking }), [booking, patchBooking, resetBooking]);

  return <ClientBookingContext.Provider value={value}>{children}</ClientBookingContext.Provider>;
}

export function useClientBooking() {
  const value = useContext(ClientBookingContext);
  if (!value) {
    throw new Error("useClientBooking must be used inside ClientBookingProvider");
  }

  return value;
}
