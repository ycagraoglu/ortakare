import type { AuthUser } from "@/features/auth/model/auth-types";

const REFRESH_TOKEN_KEY = "ortakare.auth.refresh-token";
const USER_KEY = "ortakare.auth.user";

function getStorage(persistent: boolean): Storage {
  return persistent ? window.localStorage : window.sessionStorage;
}

function clearFrom(storage: Storage): void {
  storage.removeItem(REFRESH_TOKEN_KEY);
  storage.removeItem(USER_KEY);
}

export function saveStoredSession(options: {
  refreshToken: string;
  user: AuthUser;
  persistent: boolean;
}): void {
  clearStoredSession();
  const storage = getStorage(options.persistent);
  storage.setItem(REFRESH_TOKEN_KEY, options.refreshToken);
  storage.setItem(USER_KEY, JSON.stringify(options.user));
}

export function updateStoredRefreshToken(refreshToken: string): void {
  const storage = window.localStorage.getItem(REFRESH_TOKEN_KEY)
    ? window.localStorage
    : window.sessionStorage;

  storage.setItem(REFRESH_TOKEN_KEY, refreshToken);
}

export function getStoredRefreshToken(): string | null {
  return (
    window.localStorage.getItem(REFRESH_TOKEN_KEY) ??
    window.sessionStorage.getItem(REFRESH_TOKEN_KEY)
  );
}

export function getStoredUser(): AuthUser | null {
  const raw =
    window.localStorage.getItem(USER_KEY) ??
    window.sessionStorage.getItem(USER_KEY);

  if (!raw) return null;

  try {
    const parsed: unknown = JSON.parse(raw);
    if (
      typeof parsed === "object" &&
      parsed !== null &&
      "id" in parsed &&
      "displayName" in parsed &&
      "email" in parsed &&
      typeof parsed.id === "string" &&
      typeof parsed.displayName === "string" &&
      typeof parsed.email === "string"
    ) {
      return {
        id: parsed.id,
        displayName: parsed.displayName,
        email: parsed.email,
      };
    }
  } catch {
    clearStoredSession();
  }

  return null;
}

export function clearStoredSession(): void {
  clearFrom(window.localStorage);
  clearFrom(window.sessionStorage);
}
