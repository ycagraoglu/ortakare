export interface AuthUser {
  id: string;
  displayName: string;
  email: string;
}

export interface AuthTokens {
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse extends AuthTokens {
  userId: string;
  displayName: string;
  email: string;
}

export type RefreshResponse = AuthTokens;

export type AuthStatus = "initializing" | "authenticated" | "anonymous";

export interface AuthSnapshot {
  status: AuthStatus;
  user: AuthUser | null;
}
