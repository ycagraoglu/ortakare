export { installGlobalErrorReporting, reportClientError } from "@/shared/observability/report-error";
export { sanitizeApiPath, sanitizeErrorMessage, sanitizeRoute } from "@/shared/observability/sanitize-telemetry";
export { createTelemetryContext, reportTelemetry } from "@/shared/observability/telemetry";
export type { TelemetryEvent } from "@/shared/observability/telemetry-types";
export { observeWebVitals } from "@/shared/observability/web-vitals";
