type AccessTokenProvider = () => string | null;
type UnauthorizedHandler = () => void | Promise<void>;

let accessTokenProvider: AccessTokenProvider = () => null;
let unauthorizedHandler: UnauthorizedHandler = () => undefined;

export function configureApiAuthentication(options: {
  getAccessToken: AccessTokenProvider;
  onUnauthorized: UnauthorizedHandler;
}): void {
  accessTokenProvider = options.getAccessToken;
  unauthorizedHandler = options.onUnauthorized;
}

export function getApiAccessToken(): string | null {
  return accessTokenProvider();
}

export async function handleApiUnauthorized(): Promise<void> {
  await unauthorizedHandler();
}
