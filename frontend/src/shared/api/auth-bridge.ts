type AccessTokenProvider = () => string | null;
type RefreshAccessTokenHandler = () => Promise<string | null>;
type SessionExpiredHandler = () => void | Promise<void>;

let accessTokenProvider: AccessTokenProvider = () => null;
let refreshAccessTokenHandler: RefreshAccessTokenHandler = async () => null;
let sessionExpiredHandler: SessionExpiredHandler = () => undefined;

export function configureApiAuthentication(options: {
  getAccessToken: AccessTokenProvider;
  refreshAccessToken: RefreshAccessTokenHandler;
  onSessionExpired: SessionExpiredHandler;
}): void {
  accessTokenProvider = options.getAccessToken;
  refreshAccessTokenHandler = options.refreshAccessToken;
  sessionExpiredHandler = options.onSessionExpired;
}

export function getApiAccessToken(): string | null {
  return accessTokenProvider();
}

export async function refreshApiAccessToken(): Promise<string | null> {
  return refreshAccessTokenHandler();
}

export async function handleApiSessionExpired(): Promise<void> {
  await sessionExpiredHandler();
}
