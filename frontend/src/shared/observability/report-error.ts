import { ApiError } from "@/shared/api/api-error";
import { sanitizeErrorMessage } from "@/shared/observability/sanitize-telemetry";
import { createTelemetryContext, reportTelemetry } from "@/shared/observability/telemetry";

interface ReportClientErrorOptions {
  source: "error-boundary" | "window-error" | "unhandled-rejection" | "api";
  correlationId?: string;
}

export function reportClientError(error: unknown, options: ReportClientErrorOptions): void {
  const normalized = error instanceof Error ? error : new Error(String(error));
  const correlationId = options.correlationId ?? (error instanceof ApiError ? error.correlationId : undefined);

  reportTelemetry({
    ...createTelemetryContext(),
    type: "frontend-error",
    level: "error",
    source: options.source,
    errorName: normalized.name.slice(0, 100),
    errorMessage: sanitizeErrorMessage(normalized.message || "Unknown error"),
    ...(correlationId ? { correlationId: correlationId.slice(0, 100) } : {}),
  });
}

export function installGlobalErrorReporting(): () => void {
  const onError = (event: ErrorEvent) => {
    reportClientError(event.error ?? event.message, { source: "window-error" });
  };

  const onUnhandledRejection = (event: PromiseRejectionEvent) => {
    reportClientError(event.reason, { source: "unhandled-rejection" });
  };

  window.addEventListener("error", onError);
  window.addEventListener("unhandledrejection", onUnhandledRejection);

  return () => {
    window.removeEventListener("error", onError);
    window.removeEventListener("unhandledrejection", onUnhandledRejection);
  };
}
