"use client";

import { useParams, useRouter } from "next/navigation";
import { useEffect } from "react";

export default function LegacyPetRedirectPage() {
  const router = useRouter();
  const params = useParams<{ petId: string }>();
  useEffect(() => { if (params?.petId) router.replace(`/dashboard/pets/${params.petId}`); }, [params, router]);
  return null;
}
