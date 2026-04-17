import type { CatalogBreed, CatalogCoatType, CatalogSizeCategory, ClientAppointmentSummary, ClientBookableOffer, ClientPetSummary, PublicBookingPlanner, PublicPlannerGroomer, PublicPlannerSlot, PublicQuotePreview, PublicPetPayload } from "@/lib/types";
import type { DisplayGroomer, DisplayServiceTemplate } from "@/lib/display-data";

export type BookingPetDraft = {
  source: "saved" | "custom";
  savedPetId?: string | null;
  name: string;
  animalTypeId?: string | null;
  animalTypeCode?: string | null;
  breedId?: string | null;
  breedName?: string | null;
  coatTypeId?: string | null;
  coatTypeName?: string | null;
  sizeCategoryId?: string | null;
  sizeCategoryName?: string | null;
  weightKg?: number | null;
  notes?: string | null;
  mixedBreed?: boolean;
};

export type BookingContactDraft = {
  fullName: string;
  phone: string;
  instagram: string;
  comments: string;
  createAccount: boolean;
  consent: boolean;
};

export type BookingSelectionMode = "exact" | "preferred_window";

export type ResolvedSelectedOffer = {
  templateId: string;
  offerId: string;
  displayName: string;
};

export type BookingState = {
  selectedTemplateIds: string[];
  pet: BookingPetDraft | null;
  preferredDate: string | null;
  preferredSlot: string | null;
  selectedSlotStartUtc: string | null;
  selectedSlotEndUtc: string | null;
  preferredGroomerId: string | null;
  preferredGroomerName: string | null;
  selectionMode: BookingSelectionMode;
  contact: BookingContactDraft;
  quote: PublicQuotePreview | null;
  planner: PublicBookingPlanner | null;
  resolvedOffers: ResolvedSelectedOffer[];
  lastCreatedRequestId: string | null;
};

export const emptyBookingState: BookingState = {
  selectedTemplateIds: [],
  pet: null,
  preferredDate: null,
  preferredSlot: null,
  selectedSlotStartUtc: null,
  selectedSlotEndUtc: null,
  preferredGroomerId: null,
  preferredGroomerName: null,
  selectionMode: "exact",
  contact: {
    fullName: "",
    phone: "",
    instagram: "",
    comments: "",
    createAccount: false,
    consent: false
  },
  quote: null,
  planner: null,
  resolvedOffers: [],
  lastCreatedRequestId: null
};

export function getAppointmentStatusLabel(status: string) {
  const normalized = status.toLowerCase();
  if (["confirmed", "proposed", "checkedin", "inprogress"].includes(normalized)) {
    return { label: "Підтверджено", tone: "default" as const };
  }
  if (["completed", "closed"].includes(normalized)) {
    return { label: "Завершено", tone: "outline" as const };
  }
  if (["cancelled", "noshow"].includes(normalized)) {
    return { label: "Скасовано", tone: "destructive" as const };
  }
  return { label: "Очікується", tone: "secondary" as const };
}

export function mapPetSummaryToBookingPet(pet: ClientPetSummary): BookingPetDraft {
  return {
    source: "saved",
    savedPetId: pet.id,
    name: pet.name,
    animalTypeCode: pet.animalTypeCode,
    breedName: pet.breedName,
    coatTypeName: pet.coatTypeCode ?? undefined,
    sizeCategoryName: pet.sizeCategoryCode ?? undefined,
    notes: pet.notes ?? undefined
  };
}

export function toPublicPetPayload(pet: BookingPetDraft | null): PublicPetPayload | null {
  if (!pet) {
    return null;
  }

  if (pet.source === "saved" && pet.savedPetId) {
    return { petId: pet.savedPetId };
  }

  return {
    petName: pet.name || null,
    animalTypeId: pet.animalTypeId ?? null,
    breedId: pet.breedId ?? null,
    coatTypeId: pet.coatTypeId ?? null,
    sizeCategoryId: pet.sizeCategoryId ?? null,
    weightKg: pet.weightKg ?? null,
    notes: pet.notes ?? null
  };
}

export function formatCurrency(amount: number, currency = "грн") {
  return `${amount.toLocaleString("uk-UA")} ${currency}`;
}

export function formatDateLong(value: string | Date) {
  return new Intl.DateTimeFormat("uk-UA", {
    day: "numeric",
    month: "long",
    year: "numeric"
  }).format(typeof value === "string" ? new Date(value) : value);
}

export function formatTime(value: string | Date) {
  return new Intl.DateTimeFormat("uk-UA", {
    hour: "2-digit",
    minute: "2-digit"
  }).format(typeof value === "string" ? new Date(value) : value);
}

export function slotLabelFromUtc(startAtUtc: string, endAtUtc: string) {
  return `${formatTime(startAtUtc)} – ${formatTime(endAtUtc)}`;
}

export function slotKey(slot: PublicPlannerSlot) {
  return `${slot.startAtUtc}__${slot.endAtUtc}`;
}

export function buildPreferredTimes(slot: PublicPlannerSlot | null | undefined) {
  if (!slot) {
    return [];
  }

  return [
    {
      startAtUtc: slot.startAtUtc,
      endAtUtc: slot.endAtUtc,
      label: slotLabelFromUtc(slot.startAtUtc, slot.endAtUtc)
    }
  ];
}

export function extractUniqueSlots(planner: PublicBookingPlanner | null): PublicPlannerSlot[] {
  if (!planner) {
    return [];
  }

  const map = new Map<string, PublicPlannerSlot>();
  const push = (slot: PublicPlannerSlot) => map.set(slotKey(slot), slot);
  planner.anySuitableSlots.forEach(push);
  planner.groomers.forEach((groomer) => groomer.slots.forEach(push));
  return Array.from(map.values()).sort((a, b) => a.startAtUtc.localeCompare(b.startAtUtc));
}

function normalized(text: string) {
  return text.toLowerCase().replace(/[ʼ']/g, "").replace(/[^a-zа-яіїє0-9]+/gi, " ").trim();
}

export function resolveOffersFromTemplates(templates: DisplayServiceTemplate[], availableOffers: ClientBookableOffer[]) {
  return templates
    .map((template) => {
      const words = new Set([normalized(template.name), ...template.keywords.map(normalized)]);
      const ranked = availableOffers
        .map((offer) => {
          const hay = normalized(`${offer.displayName} ${offer.offerType}`);
          let score = 0;
          for (const word of words) {
            if (word && hay.includes(word)) {
              score += Math.max(8, word.length);
            }
          }
          score -= Math.abs((offer.priceAmount ?? 0) - template.priceFrom) / 40;
          return { offer, score };
        })
        .sort((a, b) => b.score - a.score);

      const best = ranked[0];
      if (!best || best.score < 5) {
        return null;
      }

      return {
        templateId: template.id,
        offerId: best.offer.id,
        displayName: best.offer.displayName
      };
    })
    .filter((value): value is ResolvedSelectedOffer => Boolean(value));
}

export function mergeGroomerCardData(real: PublicPlannerGroomer, profiles: DisplayGroomer[]) {
  const direct = profiles.find((profile) => profile.name.toLowerCase() === real.displayName.toLowerCase());
  const fallback = profiles.find((profile) => normalized(real.displayName).includes(normalized(profile.name)) || normalized(profile.name).includes(normalized(real.displayName)));
  return direct ?? fallback ?? {
    id: real.groomerId,
    name: real.displayName,
    avatar: undefined,
    specialties: [],
    experience: "Майстер Tailbook",
    bio: "Доступний майстер салону"
  };
}

export function findBreedById(breeds: CatalogBreed[], breedId?: string | null) {
  return breeds.find((item) => item.id === breedId) ?? null;
}

export function findCoatById(coats: CatalogCoatType[], coatId?: string | null) {
  return coats.find((item) => item.id === coatId) ?? null;
}

export function findSizeById(sizes: CatalogSizeCategory[], sizeId?: string | null) {
  return sizes.find((item) => item.id === sizeId) ?? null;
}

export function appointmentCanBeRepeated(appointment: ClientAppointmentSummary) {
  const state = appointment.status.toLowerCase();
  return ["completed", "closed", "confirmed", "proposed"].includes(state);
}
