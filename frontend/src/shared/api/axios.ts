import axios, {
  AxiosHeaders,
  type AxiosError,
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
    if (axios.isAxiosError(error)) {
      const axiosError = error as AxiosError;
      const config = axiosError.config;

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
    }

    return Promise.reject(normalizeApiError(error));
  },
);
