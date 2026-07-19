import axios, {
  AxiosHeaders,
  type InternalAxiosRequestConfig,
} from "axios";

import { getApiAccessToken, handleApiUnauthorized } from "@/shared/api/auth-bridge";
import { createCorrelationId } from "@/shared/api/correlation-id";
import { normalizeApiError } from "@/shared/api/api-error";
import { env } from "@/shared/config/env";

const DEFAULT_TIMEOUT_MS = 30_000;

export const apiClient = axios.create({
  baseURL: env.VITE_API_URL,
  timeout: DEFAULT_TIMEOUT_MS,
  headers: {
    Accept: "application/json",
  },
});

apiClient.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const headers = AxiosHeaders.from(config.headers);
  const accessToken = getApiAccessToken();

  headers.set("X-Correlation-Id", createCorrelationId());

  if (accessToken) {
    headers.set("Authorization", `Bearer ${accessToken}`);
  }

  config.headers = headers;
  return config;
});

apiClient.interceptors.response.use(
  (response) => response,
  async (error: unknown) => {
    const normalizedError = normalizeApiError(error);

    if (normalizedError.kind === "unauthorized") {
      await handleApiUnauthorized();
    }

    return Promise.reject(normalizedError);
  },
);
