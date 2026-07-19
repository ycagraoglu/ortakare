import { configureApiAuthentication } from "@/shared/api/auth-bridge";
import { login, logout, refresh } from "@/features/auth/api/auth-api";
import {
  clearStoredSession,
  getStoredRefreshToken,
  getStoredUser,
  saveStoredSession,
  updateStoredRefreshToken,
} from "@/features/auth/model/auth-storage";
import type {
  AuthSnapshot,
  AuthTokens,
  AuthUser,
  LoginRequest,
} from "@/features/auth/model/auth-types";

let accessToken: string | null = null;
let refreshPromise: Promise<string | null> | null = null;
let snapshot: AuthSnapshot = {
  status: "initializing",
  user: getStoredUser(),
};

const listeners = new Set<() => void>();

function emit(next: AuthSnapshot): void {
  snapshot = next;
  for (const listener of listeners) listener();
}

function applyTokens(tokens: AuthTokens): void {
  accessToken = tokens.accessToken;
  updateStoredRefreshToken(tokens.refreshToken);
}

function clearSession(): void {
  accessToken = null;
  refreshPromise = null;
  clearStoredSession();
  emit({ status: "anonymous", user: null });
}

async function refreshAccessToken(): Promise<string | null> {
  if (refreshPromise) return refreshPromise;

  const refreshToken = getStoredRefreshToken();
  if (!refreshToken) return null;

  refreshPromise = refresh(refreshToken)
    .then((tokens) => {
      applyTokens(tokens);
      return tokens.accessToken;
    })
    .catch(() => {
      clearSession();
      return null;
    })
    .finally(() => {
      refreshPromise = null;
    });

  return refreshPromise;
}

configureApiAuthentication({
  getAccessToken: () => accessToken,
  refreshAccessToken,
  onSessionExpired: clearSession,
});

export const authSession = {
  getSnapshot(): AuthSnapshot {
    return snapshot;
  },

  subscribe(listener: () => void): () => void {
    listeners.add(listener);
    return () => listeners.delete(listener);
  },

  async restore(): Promise<void> {
    const storedUser = getStoredUser();
    const token = await refreshAccessToken();

    if (token && storedUser) {
      emit({ status: "authenticated", user: storedUser });
      return;
    }

    clearSession();
  },

  async login(request: LoginRequest, persistent: boolean): Promise<void> {
    const response = await login(request);
    const user: AuthUser = {
      id: response.userId,
      displayName: response.displayName,
      email: response.email,
    };

    accessToken = response.accessToken;
    saveStoredSession({
      refreshToken: response.refreshToken,
      user,
      persistent,
    });
    emit({ status: "authenticated", user });
  },

  async logout(): Promise<void> {
    const refreshToken = getStoredRefreshToken();

    try {
      if (refreshToken) await logout(refreshToken);
    } finally {
      clearSession();
    }
  },
};
