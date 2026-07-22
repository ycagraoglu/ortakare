import type { AuthUser } from "@/features/auth/model/auth-types";

const REFRESH_TOKEN_KEY = "ortakare.auth.refresh-token";
const USER_KEY = "ortakare.auth.user";
const REMEMBERED_EMAIL_KEY = "ortakare.auth.remembered-email";

function clearSessionStorage(): void {
  window.sessionStorage.removeItem(REFRESH_TOKEN_KEY);
  window.sessionStorage.removeItem(USER_KEY);
}

export function saveStoredSession(options: {
  refreshToken: string;
  user: AuthUser;
}): void {
  clearStoredSession();
  window.sessionStorage.setItem(REFRESH_TOKEN_KEY, options.refreshToken);
  window.sessionStorage.setItem(USER_KEY, JSON.stringify(options.user));
}

export function updateStoredRefreshToken(refreshToken: string): void {
  window.sessionStorage.setItem(REFRESH_TOKEN_KEY, refreshToken);
}

export function getStoredRefreshToken(): string | null {
  return window.sessionStorage.getItem(REFRESH_TOKEN_KEY);
}

export function getStoredUser(): AuthUser | null {
  const raw = window.sessionStorage.getItem(USER_KEY);

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

export function saveRememberedEmail(email: string | null): void {
  if (!email) {
    window.localStorage.removeItem(REMEMBERED_EMAIL_KEY);
    return;
  }

  window.localStorage.setItem(REMEMBERED_EMAIL_KEY, email.trim().toLowerCase());
}

export function getRememberedEmail(): string {
  return window.localStorage.getItem(REMEMBERED_EMAIL_KEY) ?? "";
}

export function clearStoredSession(): void {
  clearSessionStorage();
  window.localStorage.removeItem(REFRESH_TOKEN_KEY);
  window.localStorage.removeItem(USER_KEY);
}
