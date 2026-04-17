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
