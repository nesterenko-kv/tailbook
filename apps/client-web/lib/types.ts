export type AuthenticatedUserView = {
    id: string;
    subjectId: string;
    email: string;
    displayName: string;
    status: string;
    clientId?: string | null;
    contactPersonId?: string | null;
    roles: string[];
    permissions: string[];
};

export type ClientLoginResponse = {
    accessToken: string;
    expiresAtUtc: string;
    user: AuthenticatedUserView;
};

export type ClientMeResponse = {
    userId: string;
    clientId: string;
    contactPersonId: string;
    email: string;
    displayName: string;
    roles: string[];
    permissions: string[];
};

export type ClientPetSummary = {
    id: string;
    name: string;
    animalTypeCode: string;
    animalTypeName: string;
    breedName: string;
    coatTypeCode?: string | null;
    sizeCategoryCode?: string | null;
    notes?: string | null;
    primaryPhotoFileName?: string | null;
};

export type ClientPetDetail = {
    id: string;
    name: string;
    animalTypeCode: string;
    animalTypeName: string;
    breedName: string;
    coatTypeCode?: string | null;
    sizeCategoryCode?: string | null;
    birthDate?: string | null;
    weightKg?: number | null;
    notes?: string | null;
    photos: { id: string; fileName: string; contentType: string; isPrimary: boolean; sortOrder: number }[];
};

export type ClientBookableOffer = {
    id: string;
    offerType: string;
    displayName: string;
    currency: string;
    priceAmount: number;
    serviceMinutes: number;
    reservedMinutes: number;
};

export type ClientAppointmentSummary = {
    id: string;
    petId: string;
    petName: string;
    startAtUtc: string;
    endAtUtc: string;
    status: string;
    itemLabels: string[];
};

export type ClientAppointmentDetail = {
    id: string;
    bookingRequestId?: string | null;
    petId: string;
    breedName: string;
    startAtUtc: string;
    endAtUtc: string;
    status: string;
    items: { id: string; itemType: string; offerDisplayName: string; priceAmount: number; serviceMinutes: number; reservedMinutes: number }[];
    totalAmount: number;
    serviceMinutes: number;
    reservedMinutes: number;
    cancellationReasonCode?: string | null;
    cancellationNotes?: string | null;
    cancelledAtUtc?: string | null;
};

export type ClientContactPreferences = {
    contactPersonId: string;
    clientId: string;
    firstName: string;
    lastName?: string | null;
    methods: { id: string; methodType: string; displayValue: string; isPreferred: boolean; verificationStatus: string; notes?: string | null }[];
};


export type PetCatalog = {
    animalTypes: CatalogAnimalType[];
    breedGroups: CatalogBreedGroup[];
    breeds: CatalogBreed[];
    coatTypes: CatalogCoatType[];
    sizeCategories: CatalogSizeCategory[];
};

export type CatalogAnimalType = { id: string; code: string; name: string };
export type CatalogBreedGroup = { id: string; animalTypeId: string; code: string; name: string };
export type CatalogBreed = { id: string; animalTypeId: string; breedGroupId?: string | null; code: string; name: string; allowedCoatTypeIds: string[]; allowedSizeCategoryIds: string[] };
export type CatalogCoatType = { id: string; animalTypeId?: string | null; code: string; name: string };
export type CatalogSizeCategory = { id: string; animalTypeId?: string | null; code: string; name: string; minWeightKg?: number | null; maxWeightKg?: number | null };

export type PublicPetPayload = {
    petId?: string | null;
    animalTypeId?: string | null;
    breedId?: string | null;
    coatTypeId?: string | null;
    sizeCategoryId?: string | null;
    weightKg?: number | null;
    petName?: string | null;
    notes?: string | null;
};

export type PublicBookableOffer = ClientBookableOffer;

export type PublicQuotePreview = {
    currency: string;
    totalAmount: number;
    serviceMinutes: number;
    reservedMinutes: number;
    items: Array<{ offerId: string; offerType: string; displayName: string; priceAmount: number; serviceMinutes: number; reservedMinutes: number }>;
    priceLines: Array<{ label: string; amount: number }>;
    durationLines: Array<{ label: string; minutes: number }>;
};

export type PublicPlannerSlot = {
    startAtUtc: string;
    endAtUtc: string;
    groomerIds: string[];
};

export type PublicPlannerGroomer = {
    groomerId: string;
    displayName: string;
    canTakeRequest: boolean;
    reservedMinutes: number;
    reasons: string[];
    slots: PublicPlannerSlot[];
};

export type PublicBookingPlanner = {
    quote: PublicQuotePreview;
    anySuitableSlots: PublicPlannerSlot[];
    groomers: PublicPlannerGroomer[];
};

export type BookingRequestDetail = {
    id: string;
    clientId?: string | null;
    petId?: string | null;
    requestedByContactId?: string | null;
    preferredGroomerId?: string | null;
    preferredGroomerName?: string | null;
    selectionMode?: string | null;
    channel: string;
    status: string;
    subject?: {
        petDisplayName?: string | null;
        animalTypeCode?: string | null;
        breedName?: string | null;
        requesterDisplayName?: string | null;
        requesterPrimaryContact?: string | null;
        preferredGroomerName?: string | null;
    } | null;
    preferredTimes: Array<{ startAtUtc: string; endAtUtc: string; label?: string | null }>;
    notes?: string | null;
    items: Array<{ id: string; offerId: string; offerVersionId?: string | null; itemType?: string | null; requestedNotes?: string | null }>;
    createdAtUtc: string;
    updatedAtUtc: string;
};
