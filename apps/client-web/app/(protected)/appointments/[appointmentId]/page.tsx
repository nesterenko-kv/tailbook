"use client";

import { useParams, useRouter } from "next/navigation";
import { useEffect } from "react";

export default function LegacyAppointmentRedirectPage() {
  const router = useRouter();
  const params = useParams<{ appointmentId: string }>();
  useEffect(() => { if (params?.appointmentId) router.replace(`/dashboard/appointments/${params.appointmentId}`); }, [params, router]);
  return null;
}
