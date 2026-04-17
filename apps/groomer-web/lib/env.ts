const publicApiBaseUrl = process.env.NEXT_PUBLIC_API_BASE_URL ?? "https://localhost:5001";
const internalApiBaseUrl = process.env.INTERNAL_API_BASE_URL ?? publicApiBaseUrl;

export function resolveApiBaseUrl() {
    return typeof window === "undefined" ? internalApiBaseUrl : publicApiBaseUrl;
}

export const env = {
    publicApiBaseUrl,
    internalApiBaseUrl
};
