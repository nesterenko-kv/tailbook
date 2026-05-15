import { createApiBaseUrlResolver } from "@tailbook/frontend-api";

const apiBaseUrl = createApiBaseUrlResolver({
    publicApiBaseUrl: process.env.NEXT_PUBLIC_API_BASE_URL,
    internalApiBaseUrl: process.env.INTERNAL_API_BASE_URL
});

export const resolveApiBaseUrl = apiBaseUrl.resolveApiBaseUrl;

export const env = {
    publicApiBaseUrl: apiBaseUrl.publicApiBaseUrl,
    internalApiBaseUrl: apiBaseUrl.internalApiBaseUrl
};
