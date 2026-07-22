import { env } from "@/shared/config/env";
import { sanitizeRoute } from "@/shared/observability/sanitize-telemetry";
import type { TelemetryContext, TelemetryEvent } from "@/shared/observability/telemetry-types";

const SESSION_ID_KEY = "ortakare.telemetry.session-id";

function getSessionId(): string {
  const current = window.sessionStorage.getItem(SESSION_ID_KEY);
  if (current) return current;

  const next = crypto.randomUUID();
  window.sessionStorage.setItem(SESSION_ID_KEY, next);
  return next;
}

export function createTelemetryContext(): TelemetryContext {
  return {
    route: sanitizeRoute(window.location.pathname),
    ...(env.VITE_RELEASE ? { release: env.VITE_RELEASE } : {}),
    sessionId: getSessionId(),
    timestampUtc: new Date().toISOString(),
  };
}

export function reportTelemetry(event: TelemetryEvent): void {
  if (import.meta.env.DEV) {
    console.debug("[telemetry]", event);
  }

  const endpoint = env.VITE_TELEMETRY_URL;
  if (!endpoint || !import.meta.env.PROD) return;

  const payload = JSON.stringify(event);
  const body = new Blob([payload], { type: "application/json" });

  if (navigator.sendBeacon(endpoint, body)) return;

  void fetch(endpoint, {
    method: "POST",
    body: payload,
    headers: { "Content-Type": "application/json" },
    credentials: "omit",
    keepalive: true,
    mode: "cors",
  }).catch(() => undefined);
}
