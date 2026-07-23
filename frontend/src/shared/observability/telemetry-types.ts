export type TelemetryLevel = "info" | "warning" | "error";

export interface TelemetryContext {
  route: string;
  release?: string;
  sessionId: string;
  timestampUtc: string;
}

export interface ErrorTelemetryEvent extends TelemetryContext {
  type: "frontend-error";
  level: "error";
  errorName: string;
  errorMessage: string;
  correlationId?: string;
  source: "error-boundary" | "window-error" | "unhandled-rejection" | "api";
}

export interface ApiTelemetryEvent extends TelemetryContext {
  type: "api-request";
  level: TelemetryLevel;
  method: string;
  path: string;
  durationMs: number;
  statusCode?: number;
  outcome: "success" | "error" | "cancelled";
  correlationId?: string;
}

export interface WebVitalTelemetryEvent extends TelemetryContext {
  type: "web-vital";
  level: "info";
  metric: "CLS" | "FCP" | "INP" | "LCP" | "TTFB";
  value: number;
  rating: "good" | "needs-improvement" | "poor";
}

export interface RouteTelemetryEvent extends TelemetryContext {
  type: "route-view";
  level: "info";
  title: string;
}

export type TelemetryEvent =
  | ErrorTelemetryEvent
  | ApiTelemetryEvent
  | WebVitalTelemetryEvent
  | RouteTelemetryEvent;
