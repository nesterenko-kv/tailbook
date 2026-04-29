export type LoginResponse = {
    accessToken: string;
    expiresAtUtc: string;
    user: {
        id: string;
        email: string;
        displayName: string;
        roles: string[];
        permissions: string[];
    };
};

export type MeResponse = {
    userId?: string | null;
    subjectId: string;
    email: string;
    displayName: string;
    clientId?: string | null;
    contactPersonId?: string | null;
    roles: string[];
    permissions: string[];
};

export type PagedResult<T> = {
    items: T[];
    page: number;
    pageSize: number;
    totalCount: number;
};

export type ItemsEnvelope<T> = {
    items: T[];
};

export type ClientListItem = {
    id: string;
    displayName: string;
    status: string;
    contactCount: number;
    createdAtUtc: string;
    updatedAtUtc: string;
};

export type ContactMethod = {
    id: string;
    methodType: string;
    displayValue: string;
    isPreferred: boolean;
    verificationStatus: string;
    notes?: string | null;
};

export type ContactPerson = {
    id: string;
    clientId: string;
    firstName: string;
    lastName?: string | null;
    notes?: string | null;
    trustLevel: string;
    methods: ContactMethod[];
};

export type ClientPetSummary = {
    id: string;
    name: string;
    animalTypeCode: string;
    animalTypeName: string;
    breedName: string;
    coatTypeCode?: string | null;
    sizeCategoryCode?: string | null;
};

export type ClientDetail = {
    id: string;
    displayName: string;
    status: string;
    notes?: string | null;
    contacts: ContactPerson[];
    pets: ClientPetSummary[];
    createdAtUtc: string;
    updatedAtUtc: string;
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

export type NamedCatalogItem = { id: string; code: string; name: string };
export type BreedItem = { id: string; animalTypeId: string; breedGroupId?: string | null; code: string; name: string };
export type SizeCategoryItem = { id: string; code: string; name: string; minWeightKg?: number | null; maxWeightKg?: number | null };

export type PetDetail = {
    id: string;
    clientId?: string | null;
    name: string;
    animalType: NamedCatalogItem;
    breed: BreedItem;
    coatType?: NamedCatalogItem | null;
    sizeCategory?: SizeCategoryItem | null;
    birthDate?: string | null;
    weightKg?: number | null;
    notes?: string | null;
    photos: PetPhoto[];
    contacts: PetContact[];
    createdAtUtc: string;
    updatedAtUtc: string;
};

export type PetPhoto = {
    id: string;
    storageKey: string;
    fileName: string;
    contentType: string;
    isPrimary: boolean;
    sortOrder: number;
    createdAtUtc: string;
};

export type PetContact = {
    contactId: string;
    clientId: string;
    fullName: string;
    isPrimary: boolean;
    canPickUp: boolean;
    canPay: boolean;
    receivesNotifications: boolean;
    roleCodes: string[];
    methods: ContactMethod[];
};

export type ProcedureItem = {
    id: string;
    code: string;
    name: string;
    isActive: boolean;
    createdAtUtc: string;
    updatedAtUtc: string;
};

export type OfferVersionComponent = {
    id: string;
    offerVersionId: string;
    procedureId: string;
    procedureCode: string;
    procedureName: string;
    componentRole: string;
    sequenceNo: number;
    defaultExpected: boolean;
    createdAtUtc: string;
};

export type OfferVersion = {
    id: string;
    offerId: string;
    versionNo: number;
    status: string;
    validFromUtc: string;
    validToUtc?: string | null;
    policyText?: string | null;
    changeNote?: string | null;
    createdAtUtc: string;
    publishedAtUtc?: string | null;
    components: OfferVersionComponent[];
};

export type Offer = {
    id: string;
    code: string;
    offerType: string;
    displayName: string;
    isActive: boolean;
    versions: OfferVersion[];
    createdAtUtc: string;
    updatedAtUtc: string;
};

export type OfferListItem = {
    id: string;
    code: string;
    offerType: string;
    displayName: string;
    isActive: boolean;
    versionCount: number;
    hasPublishedVersion: boolean;
    createdAtUtc: string;
    updatedAtUtc: string;
};

export type RuleConditionPayload = {
    animalTypeId?: string | null;
    breedId?: string | null;
    breedGroupId?: string | null;
    coatTypeId?: string | null;
    sizeCategoryId?: string | null;
};

export type PriceRule = {
    id: string;
    ruleSetId: string;
    offerId: string;
    offerCode: string;
    offerDisplayName: string;
    priority: number;
    specificityScore: number;
    actionType: string;
    fixedAmount: number;
    currency: string;
    condition: RuleConditionPayload;
    createdAtUtc: string;
};

export type PriceRuleSet = {
    id: string;
    versionNo: number;
    status: string;
    validFromUtc: string;
    validToUtc?: string | null;
    createdAtUtc: string;
    publishedAtUtc?: string | null;
    rules: PriceRule[];
};

export type DurationRule = {
    id: string;
    ruleSetId: string;
    offerId: string;
    offerCode: string;
    offerDisplayName: string;
    priority: number;
    specificityScore: number;
    baseMinutes: number;
    bufferBeforeMinutes: number;
    bufferAfterMinutes: number;
    condition: RuleConditionPayload;
    createdAtUtc: string;
};

export type DurationRuleSet = {
    id: string;
    versionNo: number;
    status: string;
    validFromUtc: string;
    validToUtc?: string | null;
    createdAtUtc: string;
    publishedAtUtc?: string | null;
    rules: DurationRule[];
};

export type QuotePreview = {
    priceSnapshot: {
        id: string;
        currency: string;
        totalAmount: number;
        lines: Array<{ lineType: string; label: string; amount: number; sourceRuleId?: string | null; sequenceNo: number }>;
    };
    durationSnapshot: {
        id: string;
        serviceMinutes: number;
        reservedMinutes: number;
        lines: Array<{ lineType: string; label: string; minutes: number; sourceRuleId?: string | null; sequenceNo: number }>;
    };
    items: Array<{ offerId: string; offerVersionId: string; offerCode: string; offerType: string; displayName: string; priceAmount: number; serviceMinutes: number; reservedMinutes: number }>;
};

export type GroomerListItem = {
    id: string;
    userId?: string | null;
    displayName: string;
    active: boolean;
    capabilityCount: number;
    createdAtUtc: string;
    updatedAtUtc: string;
};

export type GroomerListResponse = ItemsEnvelope<GroomerListItem>;
export type PriceRuleSetListResponse = ItemsEnvelope<PriceRuleSet>;
export type DurationRuleSetListResponse = ItemsEnvelope<DurationRuleSet>;

export type GroomerCapability = {
    id: string;
    groomerId: string;
    animalTypeId?: string | null;
    breedId?: string | null;
    breedGroupId?: string | null;
    coatTypeId?: string | null;
    sizeCategoryId?: string | null;
    offerId?: string | null;
    capabilityMode: string;
    reservedDurationModifierMinutes: number;
    notes?: string | null;
    createdAtUtc: string;
};

export type WorkingSchedule = {
    id: string;
    groomerId: string;
    weekday: number;
    startLocalTime: string;
    endLocalTime: string;
    createdAtUtc: string;
    updatedAtUtc: string;
};

export type TimeBlock = {
    id: string;
    groomerId: string;
    startAtUtc: string;
    endAtUtc: string;
    reasonCode: string;
    notes?: string | null;
    createdAtUtc: string;
};

export type GroomerDetail = {
    id: string;
    userId?: string | null;
    displayName: string;
    active: boolean;
    capabilities: GroomerCapability[];
    workingSchedules: WorkingSchedule[];
    createdAtUtc: string;
    updatedAtUtc: string;
};

export type GroomerScheduleResult = {
    groomerId: string;
    groomerDisplayName: string;
    fromUtc: string;
    toUtc: string;
    workingSchedules: WorkingSchedule[];
    timeBlocks: TimeBlock[];
    availabilityWindows: Array<{
        startAtUtc: string;
        endAtUtc: string;
    }>;
};

export type AvailabilityResult = {
    isAvailable: boolean;
    endAtUtc: string;
    checkedReservedMinutes: number;
    reasons: string[];
};

export type BookingRequestListItem = {
    id: string;
    clientId?: string | null;
    petId?: string | null;
    requestedByContactId?: string | null;
    preferredGroomerId?: string | null;
    selectionMode?: string | null;
    channel: string;
    status: string;
    itemCount: number;
    petDisplayName?: string | null;
    requesterDisplayName?: string | null;
    requesterPrimaryContact?: string | null;
    preferredGroomerName?: string | null;
    createdAtUtc: string;
    updatedAtUtc: string;
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
        guestIntake?: {
            requester?: {
                displayName?: string | null;
                phone?: string | null;
                instagramHandle?: string | null;
                email?: string | null;
                preferredContactMethodCode?: string | null;
            } | null;
            pet?: {
                displayName?: string | null;
                animalTypeName?: string | null;
                breedName?: string | null;
                coatTypeName?: string | null;
                sizeCategoryName?: string | null;
                weightKg?: number | null;
                notes?: string | null;
            } | null;
        } | null;
    } | null;
    preferredTimes: Array<{ startAtUtc: string; endAtUtc: string; label?: string | null }>;
    notes?: string | null;
    items: Array<{ id: string; offerId: string; offerVersionId?: string | null; itemType?: string | null; requestedNotes?: string | null }>;
    createdAtUtc: string;
    updatedAtUtc: string;
};

export type AppointmentListItem = {
    id: string;
    bookingRequestId?: string | null;
    petId: string;
    groomerId: string;
    startAtUtc: string;
    endAtUtc: string;
    status: string;
    versionNo: number;
    itemCount: number;
    totalAmount: number;
};

export type AppointmentDetail = {
    id: string;
    bookingRequestId?: string | null;
    pet: { id: string; clientId?: string | null; animalTypeCode: string; animalTypeName: string; breedName: string };
    groomerId: string;
    startAtUtc: string;
    endAtUtc: string;
    status: string;
    versionNo: number;
    items: Array<{ id: string; itemType: string; offerId: string; offerVersionId: string; offerCode: string; offerDisplayName: string; quantity: number; priceSnapshotId: string; durationSnapshotId: string; priceAmount: number; serviceMinutes: number; reservedMinutes: number }>;
    totalAmount: number;
    serviceMinutes: number;
    reservedMinutes: number;
    cancellationReasonCode?: string | null;
    cancellationNotes?: string | null;
    cancelledAtUtc?: string | null;
    createdAtUtc: string;
    updatedAtUtc: string;
};

export type VisitDetail = {
    id: string;
    appointmentId: string;
    bookingRequestId?: string | null;
    pet: { id: string; name: string; animalTypeCode: string; animalTypeName: string; breedName: string; coatTypeCode?: string | null; sizeCategoryCode?: string | null };
    groomerId: string;
    status: string;
    checkedInAtUtc: string;
    startedAtUtc?: string | null;
    completedAtUtc?: string | null;
    closedAtUtc?: string | null;
    serviceMinutes: number;
    reservedMinutes: number;
    appointmentTotalAmount: number;
    adjustmentTotalAmount: number;
    finalTotalAmount: number;
    items: Array<{
        id: string;
        appointmentItemId: string;
        itemType: string;
        offerId: string;
        offerVersionId: string;
        offerCode: string;
        offerDisplayName: string;
        quantity: number;
        priceAmount: number;
        serviceMinutes: number;
        reservedMinutes: number;
        expectedComponents: Array<{ id: string; procedureId: string; procedureCode: string; procedureName: string; componentRole: string; sequenceNo: number; defaultExpected: boolean; isSkipped: boolean }>;
        performedProcedures: Array<{ id: string; procedureId: string; procedureCode: string; procedureName: string; status: string; note?: string | null; recordedAtUtc: string }>;
        skippedComponents: Array<{ id: string; offerVersionComponentId: string; procedureId: string; procedureCode: string; procedureName: string; omissionReasonCode: string; note?: string | null; recordedAtUtc: string }>;
    }>;
    adjustments: Array<{ id: string; sign: number; amount: number; reasonCode: string; note?: string | null; createdAtUtc: string }>;
    createdAtUtc: string;
    updatedAtUtc: string;
};

export type AuditAccessItem = {
    id: string;
    actorUserId?: string | null;
    resourceType: string;
    resourceId: string;
    actionCode: string;
    happenedAtUtc: string;
};

export type UserListItem = {
    id: string;
    subjectId: string;
    email: string;
    displayName: string;
    status: string;
    roles: string[];
    createdAtUtc: string;
    updatedAtUtc: string;
};

export type RoleItem = {
    id: string;
    code: string;
    displayName: string;
    permissionCodes: string[];
};

export type PermissionItem = {
    id: string;
    code: string;
    displayName: string;
};
