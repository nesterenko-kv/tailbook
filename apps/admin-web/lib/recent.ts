const RECENT_PET_IDS_KEY = "tailbook_recent_pet_ids";
const RECENT_VISIT_IDS_KEY = "tailbook_recent_visit_ids";

function getRecentIds(key: string) {
  if (typeof window === "undefined") return [] as string[];
  try {
    const raw = window.localStorage.getItem(key);
    if (!raw) return [];
    const parsed = JSON.parse(raw);
    return Array.isArray(parsed) ? parsed.filter((item) => typeof item === "string") : [];
  } catch {
    return [];
  }
}

function pushRecentId(key: string, id: string) {
  if (typeof window === "undefined") return;
  const next = [id, ...getRecentIds(key).filter((item) => item !== id)].slice(0, 10);
  window.localStorage.setItem(key, JSON.stringify(next));
}

export function getRecentPetIds() {
  return getRecentIds(RECENT_PET_IDS_KEY);
}

export function addRecentPetId(id: string) {
  pushRecentId(RECENT_PET_IDS_KEY, id);
}

export function getRecentVisitIds() {
  return getRecentIds(RECENT_VISIT_IDS_KEY);
}

export function addRecentVisitId(id: string) {
  pushRecentId(RECENT_VISIT_IDS_KEY, id);
}
