import "axios";

declare module "axios" {
  export interface AxiosRequestConfig {
    skipAuthRefresh?: boolean;
    authRetryAttempted?: boolean;
    telemetryStartedAt?: number;
  }

  export interface InternalAxiosRequestConfig {
    skipAuthRefresh?: boolean;
    authRetryAttempted?: boolean;
    telemetryStartedAt?: number;
  }
}
