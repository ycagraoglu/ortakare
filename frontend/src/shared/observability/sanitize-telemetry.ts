const GUID_PATTERN = /\b[0-9a-f]{8}-[0-9a-f]{4}-[1-8][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}\b/gi;
const LONG_IDENTIFIER_PATTERN = /\b[a-z0-9_-]{24,}\b/gi;
const EMAIL_PATTERN = /\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b/gi;
const QUERY_OR_HASH_PATTERN = /[?#].*$/;

export function sanitizeRoute(value: string): string {
  const path = value.replace(QUERY_OR_HASH_PATTERN, "") || "/";
  return path
    .replace(GUID_PATTERN, ":id")
    .replace(LONG_IDENTIFIER_PATTERN, ":id")
    .replace(/\/\d+(?=\/|$)/g, "/:id");
}

export function sanitizeErrorMessage(value: string): string {
  return value
    .replace(EMAIL_PATTERN, "[email]")
    .replace(GUID_PATTERN, "[id]")
    .replace(LONG_IDENTIFIER_PATTERN, "[id]")
    .slice(0, 500);
}

export function sanitizeApiPath(value: string): string {
  try {
    return sanitizeRoute(new URL(value, window.location.origin).pathname);
  } catch {
    return sanitizeRoute(value);
  }
}
