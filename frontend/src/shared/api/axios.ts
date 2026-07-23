import axios, {
  AxiosHeaders,
  type AxiosError,
  type AxiosResponse,
  type InternalAxiosRequestConfig,
} from "axios";

import {
  getApiAccessToken,
  handleApiSessionExpired,
  refreshApiAccessToken,
} from "@/shared/api/auth-bridge";
import { createCorrelationId } from "@/shared/api/correlation-id";
import { normalizeApiError } from "@/shared/api/api-error";
import { env } from "@/shared/config/env";
import {
  createTelemetryContext,
  reportClientError,
  reportTelemetry,
  sanitizeApiPath,
} from "@/shared/observability";

const DEFAULT_TIMEOUT_MS = 30_000;
const API_ORIGIN = new URL(env.VITE_API_URL).origin;

function assertTrustedApiRequest(config: InternalAxiosRequestConfig): void {
  const requestUrl = new URL(config.url ?? "", config.baseURL ?? env.VITE_API_URL);

  if (requestUrl.origin !== API_ORIGIN) {
    throw new Error("API client rejected a request to an untrusted origin.");
  }

  if (import.meta.env.PROD && requestUrl.protocol !== "https:") {
    throw new Error("API client rejected an insecure production request.");
  }
}

function readCorrelationId(headers: unknown): string | undefined {
  const value = AxiosHeaders.from(headers).get("x-correlation-id");
  return typeof value === "string" && value.trim() ? value.slice(0, 100) : undefined;
}

function reportApiRequest(
  config: InternalAxiosRequestConfig | undefined,
  options: {
    outcome: "success" | "error" | "cancelled";
    statusCode?: number;
    correlationId?: string;
  },
): void {
  if (!config) return;

  const durationMs = Math.max(0, performance.now() - (config.telemetryStartedAt ?? performance.now()));
  const method = (config.method ?? "GET").toUpperCase();
  const requestUrl = new URL(config.url ?? "", config.baseURL ?? env.VITE_API_URL);

  reportTelemetry({
    ...createTelemetryContext(),
    type: "api-request",
    level: options.outcome === "success" ? "info" : options.outcome === "cancelled" ? "warning" : "error",
    method,
    path: sanitizeApiPath(requestUrl.pathname),
    durationMs: Math.round(durationMs),
    outcome: options.outcome,
    ...(options.statusCode ? { statusCode: options.statusCode } : {}),
    ...(options.correlationId ? { correlationId: options.correlationId } : {}),
  });
}

export const apiClient = axios.create({
  baseURL: env.VITE_API_URL,
  timeout: DEFAULT_TIMEOUT_MS,
  headers: {
    Accept: "application/json",
  },
});

apiClient.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  assertTrustedApiRequest(config);

  const headers = AxiosHeaders.from(config.headers);
  const accessToken = getApiAccessToken();

  headers.set("X-Correlation-Id", createCorrelationId());

  if (accessToken) {
    headers.set("Authorization", `Bearer ${accessToken}`);
  }

  config.telemetryStartedAt = performance.now();
  config.headers = headers;
  return config;
});

apiClient.interceptors.response.use(
  (response: AxiosResponse) => {
    reportApiRequest(response.config, {
      outcome: "success",
      statusCode: response.status,
      correlationId: readCorrelationId(response.headers),
    });
    return response;
  },
  async (error: unknown) => {
    if (axios.isAxiosError(error)) {
      const axiosError = error as AxiosError;
      const config = axiosError.config;
      const correlationId = readCorrelationId(axiosError.response?.headers);

      if (
        axiosError.response?.status === 401 &&
        config &&
        !config.skipAuthRefresh &&
        !config.authRetryAttempted
      ) {
        config.authRetryAttempted = true;
        const accessToken = await refreshApiAccessToken();

        if (accessToken) {
          const headers = AxiosHeaders.from(config.headers);
          headers.set("Authorization", `Bearer ${accessToken}`);
          config.headers = headers;
          return apiClient.request(config);
        }

        await handleApiSessionExpired();
      }

      reportApiRequest(config, {
        outcome: axiosError.code === "ERR_CANCELED" ? "cancelled" : "error",
        ...(axiosError.response?.status ? { statusCode: axiosError.response.status } : {}),
        ...(correlationId ? { correlationId } : {}),
      });

      const normalized = normalizeApiError(error);
      if (normalized.kind !== "aborted") {
        reportClientError(normalized, { source: "api", correlationId });
      }
      return Promise.reject(normalized);
    }

    reportClientError(error, { source: "api" });
    return Promise.reject(normalizeApiError(error));
  },
);
