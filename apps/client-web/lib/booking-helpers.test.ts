import { describe, it, expect } from "vitest";
import {
    getAppointmentStatusLabel,
    mapPetSummaryToBookingPet,
    toPublicPetPayload,
    formatCurrency,
    formatDateLong,
    formatTime,
    slotLabelFrom,
    slotKey,
    buildPreferredTimes,
    extractUniqueSlots,
    resolveOffersFromTemplates,
    mergeGroomerCardData,
    findBreedById,
    findCoatById,
    findSizeById,
    appointmentCanBeRepeated
} from "./booking-helpers";
import type { ClientPetSummary, ClientBookableOffer, PublicPlannerSlot, PublicBookingPlanner, PublicPlannerGroomer, CatalogBreed, CatalogCoatType, CatalogSizeCategory, ClientAppointmentSummary } from "./types";
import type { DisplayGroomer, DisplayServiceTemplate } from "./display-data";

describe("getAppointmentStatusLabel", () => {
    it("returns confirmed label for confirmed status", () => {
        const result = getAppointmentStatusLabel("confirmed");
        expect(result.label).toBe("Підтверджено");
        expect(result.tone).toBe("default");
    });

    it("returns confirmed label for checkedin status", () => {
        const result = getAppointmentStatusLabel("checkedin");
        expect(result.label).toBe("Підтверджено");
    });

    it("returns confirmed label for inprogress status", () => {
        const result = getAppointmentStatusLabel("inprogress");
        expect(result.label).toBe("Підтверджено");
    });

    it("returns completed label for completed status", () => {
        const result = getAppointmentStatusLabel("completed");
        expect(result.label).toBe("Завершено");
        expect(result.tone).toBe("outline");
    });

    it("returns completed label for closed status", () => {
        const result = getAppointmentStatusLabel("closed");
        expect(result.label).toBe("Завершено");
    });

    it("returns cancelled label for cancelled status", () => {
        const result = getAppointmentStatusLabel("cancelled");
        expect(result.label).toBe("Скасовано");
        expect(result.tone).toBe("destructive");
    });

    it("returns cancelled label for noshow status", () => {
        const result = getAppointmentStatusLabel("noshow");
        expect(result.label).toBe("Скасовано");
    });

    it("returns pending label for unknown status", () => {
        const result = getAppointmentStatusLabel("pending");
        expect(result.label).toBe("Очікується");
        expect(result.tone).toBe("secondary");
    });

    it("is case-insensitive", () => {
        expect(getAppointmentStatusLabel("COMPLETED").label).toBe("Завершено");
        expect(getAppointmentStatusLabel("Cancelled").label).toBe("Скасовано");
    });
});

describe("mapPetSummaryToBookingPet", () => {
    it("maps a full pet summary to a saved pet draft", () => {
        const pet: ClientPetSummary = {
            id: "pet-1",
            name: "Rex",
            animalTypeCode: "dog",
            animalTypeName: "Собака",
            breedName: "Labrador",
            coatTypeCode: "short",
            sizeCategoryCode: "large",
            notes: "Friendly"
        };
        const result = mapPetSummaryToBookingPet(pet);
        expect(result.source).toBe("saved");
        expect(result.savedPetId).toBe("pet-1");
        expect(result.name).toBe("Rex");
        expect(result.animalTypeCode).toBe("dog");
        expect(result.breedName).toBe("Labrador");
        expect(result.coatTypeName).toBe("short");
        expect(result.sizeCategoryName).toBe("large");
        expect(result.notes).toBe("Friendly");
    });

    it("handles missing optional fields", () => {
        const pet: ClientPetSummary = {
            id: "pet-2",
            name: "Mittens",
            animalTypeCode: "cat",
            animalTypeName: "Кіт",
            breedName: "Persian",
            coatTypeCode: undefined,
            sizeCategoryCode: undefined,
            notes: undefined
        };
        const result = mapPetSummaryToBookingPet(pet);
        expect(result.coatTypeName).toBeUndefined();
        expect(result.sizeCategoryName).toBeUndefined();
        expect(result.notes).toBeUndefined();
    });
});

describe("toPublicPetPayload", () => {
    it("returns null when pet is null", () => {
        expect(toPublicPetPayload(null)).toBeNull();
    });

    it("returns petId for saved pet", () => {
        const result = toPublicPetPayload({
            source: "saved",
            savedPetId: "pet-1",
            name: "Rex",
            animalTypeId: null,
            breedId: null,
            coatTypeId: null,
            sizeCategoryId: null,
            weightKg: null,
            notes: null,
            mixedBreed: false
        });
        expect(result).toEqual({ petId: "pet-1" });
    });

    it("returns full payload for custom pet", () => {
        const result = toPublicPetPayload({
            source: "custom",
            savedPetId: null,
            name: "Buddy",
            animalTypeId: "type-1",
            breedId: "breed-1",
            coatTypeId: "coat-1",
            sizeCategoryId: "size-1",
            weightKg: 25,
            notes: "Calm dog",
            mixedBreed: false
        });
        expect(result).toEqual({
            petName: "Buddy",
            animalTypeId: "type-1",
            breedId: "breed-1",
            coatTypeId: "coat-1",
            sizeCategoryId: "size-1",
            weightKg: 25,
            notes: "Calm dog"
        });
    });
});

describe("formatCurrency", () => {
    it("formats currency with default грн", () => {
        const result = formatCurrency(800);
        expect(result).toContain("грн");
    });

    it("accepts custom currency", () => {
        const result = formatCurrency(1200, "$");
        expect(result).toContain("$");
    });

    it("handles zero", () => {
        expect(formatCurrency(0)).toContain("грн");
    });
});

describe("formatDateLong", () => {
    it("formats a date string", () => {
        const result = formatDateLong("2026-05-14T10:00:00Z");
        expect(result).toContain("2026");
    });

    it("formats a Date object", () => {
        const result = formatDateLong(new Date("2026-05-14T10:00:00Z"));
        expect(result).toContain("2026");
    });
});

describe("formatTime", () => {
    it("formats time from string", () => {
        const result = formatTime("2026-05-14T10:00:00Z");
        expect(result).toBeTruthy();
    });
});

describe("slotLabelFrom", () => {
    it("combines start and end times", () => {
        const label = slotLabelFrom("2026-05-14T10:00:00Z", "2026-05-14T11:00:00Z");
        expect(label).toContain("–");
    });
});

describe("slotKey", () => {
    it("creates unique key from slot", () => {
        const slot: PublicPlannerSlot = { startAt: "10:00", endAt: "11:00", groomerIds: [] };
        expect(slotKey(slot)).toBe("10:00__11:00");
    });
});

describe("buildPreferredTimes", () => {
    it("returns empty array for null slot", () => {
        expect(buildPreferredTimes(null)).toEqual([]);
    });

    it("returns empty array for undefined slot", () => {
        expect(buildPreferredTimes(undefined)).toEqual([]);
    });

    it("builds preferred time from slot", () => {
        const slot: PublicPlannerSlot = { startAt: "2026-05-14T10:00", endAt: "2026-05-14T11:00", groomerIds: [] };
        const result = buildPreferredTimes(slot);
        expect(result).toHaveLength(1);
        expect(result[0].startAt).toBe("2026-05-14T10:00");
        expect(result[0].endAt).toBe("2026-05-14T11:00");
        expect(result[0].label).toBeTruthy();
    });
});

describe("extractUniqueSlots", () => {
    it("returns empty array for null planner", () => {
        expect(extractUniqueSlots(null)).toEqual([]);
    });

    it("extracts unique slots sorted by startAt", () => {
        const slotA: PublicPlannerSlot = { startAt: "10:00", endAt: "11:00", groomerIds: [] };
        const slotB: PublicPlannerSlot = { startAt: "09:00", endAt: "10:00", groomerIds: [] };
        const planner: PublicBookingPlanner = {
            quote: { currency: "грн", totalAmount: 800, serviceMinutes: 60, reservedMinutes: 15, items: [], priceLines: [], durationLines: [] },
            anySuitableSlots: [slotA],
            groomers: [{ groomerId: "g-1", displayName: "Olena", canTakeRequest: true, reservedMinutes: 60, reasons: [], slots: [slotB] }]
        };
        const result = extractUniqueSlots(planner);
        expect(result).toHaveLength(2);
        expect(result[0].startAt).toBe("09:00");
        expect(result[1].startAt).toBe("10:00");
    });
});

describe("resolveOffersFromTemplates", () => {
    const offers: ClientBookableOffer[] = [
        { id: "offer-1", offerType: "complex", displayName: "Комплекс для собак", currency: "грн", priceAmount: 800, serviceMinutes: 120, reservedMinutes: 15 },
        { id: "offer-2", offerType: "model", displayName: "Модельна стрижка", currency: "грн", priceAmount: 1200, serviceMinutes: 180, reservedMinutes: 15 }
    ];

    it("matches a template to an offer", () => {
        const templates: DisplayServiceTemplate[] = [{
            id: "tpl-1", name: "Комплекс для собак", description: "", category: "dog",
            priceFrom: 800, duration: 120, keywords: ["комплекс", "повний"]
        }];
        const result = resolveOffersFromTemplates(templates, offers);
        expect(result).toHaveLength(1);
        expect(result[0].offerId).toBe("offer-1");
    });

    it("returns empty when no offer scores high enough", () => {
        const templates: DisplayServiceTemplate[] = [{
            id: "tpl-x", name: "Rare exotic service", description: "", category: "dog",
            priceFrom: 9999, duration: 999, keywords: ["exotic", "rare"]
        }];
        const result = resolveOffersFromTemplates(templates, offers);
        expect(result).toHaveLength(0);
    });

    it("returns empty for empty templates", () => {
        const result = resolveOffersFromTemplates([], offers);
        expect(result).toHaveLength(0);
    });
});

describe("mergeGroomerCardData", () => {
    const profiles: DisplayGroomer[] = [
        { id: "g-1", name: "Олена Коваленко", specialties: [], experience: "8 років", bio: "" }
    ];

    it("finds direct match by name", () => {
        const groomer: PublicPlannerGroomer = { groomerId: "g-1", displayName: "Олена Коваленко", canTakeRequest: true, reservedMinutes: 60, reasons: [], slots: [] };
        const result = mergeGroomerCardData(groomer, profiles);
        expect(result.id).toBe("g-1");
    });

    it("falls back to generated profile when no match", () => {
        const groomer: PublicPlannerGroomer = { groomerId: "g-99", displayName: "Unknown Groomer", canTakeRequest: true, reservedMinutes: 60, reasons: [], slots: [] };
        const result = mergeGroomerCardData(groomer, profiles);
        expect(result.name).toBe("Unknown Groomer");
        expect(result.experience).toBe("Майстер Tailbook");
    });
});

describe("findBreedById", () => {
    const breeds: CatalogBreed[] = [
        { id: "b-1", animalTypeId: "t-1", code: "lab", name: "Labrador", allowedCoatTypeIds: [], allowedSizeCategoryIds: [] }
    ];

    it("finds breed by id", () => {
        expect(findBreedById(breeds, "b-1")?.name).toBe("Labrador");
    });

    it("returns null for unknown id", () => {
        expect(findBreedById(breeds, "b-99")).toBeNull();
    });

    it("returns null for null id", () => {
        expect(findBreedById(breeds, null)).toBeNull();
    });
});

describe("findCoatById", () => {
    const coats: CatalogCoatType[] = [
        { id: "c-1", code: "short", name: "Short" }
    ];

    it("finds coat by id", () => {
        expect(findCoatById(coats, "c-1")?.name).toBe("Short");
    });

    it("returns null for unknown id", () => {
        expect(findCoatById(coats, "c-99")).toBeNull();
    });
});

describe("findSizeById", () => {
    const sizes: CatalogSizeCategory[] = [
        { id: "s-1", code: "large", name: "Large" }
    ];

    it("finds size by id", () => {
        expect(findSizeById(sizes, "s-1")?.name).toBe("Large");
    });

    it("returns null for unknown id", () => {
        expect(findSizeById(sizes, "s-99")).toBeNull();
    });
});

describe("appointmentCanBeRepeated", () => {
    function makeAppointment(status: string): ClientAppointmentSummary {
        return { id: "a-1", petId: "p-1", petName: "Rex", startAt: "", endAt: "", status, itemLabels: [] };
    }

    it("allows repeat for completed", () => {
        expect(appointmentCanBeRepeated(makeAppointment("completed"))).toBe(true);
    });

    it("allows repeat for closed", () => {
        expect(appointmentCanBeRepeated(makeAppointment("closed"))).toBe(true);
    });

    it("allows repeat for confirmed", () => {
        expect(appointmentCanBeRepeated(makeAppointment("confirmed"))).toBe(true);
    });

    it("allows repeat for proposed", () => {
        expect(appointmentCanBeRepeated(makeAppointment("proposed"))).toBe(true);
    });

    it("disallows repeat for cancelled", () => {
        expect(appointmentCanBeRepeated(makeAppointment("cancelled"))).toBe(false);
    });

    it("disallows repeat for noshow", () => {
        expect(appointmentCanBeRepeated(makeAppointment("noshow"))).toBe(false);
    });

    it("disallows repeat for checkedin", () => {
        expect(appointmentCanBeRepeated(makeAppointment("checkedin"))).toBe(false);
    });

    it("is case-insensitive", () => {
        expect(appointmentCanBeRepeated(makeAppointment("COMPLETED"))).toBe(true);
    });
});
