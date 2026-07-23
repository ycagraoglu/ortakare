const SAFE_EXTERNAL_PROTOCOLS = new Set(["https:"]);

export interface TrustedUrlOptions {
  allowedOrigins?: readonly string[];
  allowSameOrigin?: boolean;
}

function normalizeOrigin(origin: string): string | null {
  try {
    return new URL(origin).origin;
  } catch {
    return null;
  }
}

export function resolveTrustedUrl(
  value: string,
  options: TrustedUrlOptions = {},
): URL | null {
  try {
    const url = new URL(value, window.location.origin);
    const allowSameOrigin = options.allowSameOrigin ?? true;

    if (url.origin === window.location.origin) {
      return allowSameOrigin ? url : null;
    }

    if (!SAFE_EXTERNAL_PROTOCOLS.has(url.protocol)) return null;

    const allowedOrigins = new Set(
      (options.allowedOrigins ?? [])
        .map(normalizeOrigin)
        .filter((origin): origin is string => origin !== null),
    );

    return allowedOrigins.has(url.origin) ? url : null;
  } catch {
    return null;
  }
}

export function openTrustedExternalUrl(
  value: string,
  allowedOrigins: readonly string[],
): boolean {
  const url = resolveTrustedUrl(value, {
    allowedOrigins,
    allowSameOrigin: false,
  });

  if (!url) return false;

  window.open(url.href, "_blank", "noopener,noreferrer");
  return true;
}

export function navigateToTrustedDownload(
  value: string,
  allowedOrigins: readonly string[],
): boolean {
  const url = resolveTrustedUrl(value, {
    allowedOrigins,
    allowSameOrigin: true,
  });

  if (!url) return false;

  window.location.assign(url.href);
  return true;
}
