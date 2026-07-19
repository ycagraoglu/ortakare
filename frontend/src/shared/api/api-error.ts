import axios from "axios";

import {
  isApiResult,
  isApiResultWithoutData,
} from "@/shared/api/api-result";

export type ApiErrorKind =
  | "aborted"
  | "network"
  | "timeout"
  | "unauthorized"
  | "forbidden"
  | "not-found"
  | "conflict"
  | "validation"
  | "rate-limit"
  | "server"
  | "unknown";

interface ApiErrorOptions {
  kind: ApiErrorKind;
  message: string;
  statusCode?: number;
  correlationId?: string;
  cause?: unknown;
}

export class ApiError extends Error {
  readonly kind: ApiErrorKind;
  readonly statusCode: number | undefined;
  readonly correlationId: string | undefined;

  constructor(options: ApiErrorOptions) {
    super(options.message, { cause: options.cause });
    this.name = "ApiError";
    this.kind = options.kind;
    this.statusCode = options.statusCode;
    this.correlationId = options.correlationId;
  }
}

function classifyStatus(statusCode: number): ApiErrorKind {
  if (statusCode === 401) return "unauthorized";
  if (statusCode === 403) return "forbidden";
  if (statusCode === 404) return "not-found";
  if (statusCode === 409) return "conflict";
  if (statusCode === 400 || statusCode === 422) return "validation";
  if (statusCode === 429) return "rate-limit";
  if (statusCode >= 500) return "server";
  return "unknown";
}

function defaultMessage(kind: ApiErrorKind): string {
  switch (kind) {
    case "aborted": return "İstek iptal edildi.";
    case "network": return "Sunucuya ulaşılamıyor. İnternet bağlantınızı kontrol edin.";
    case "timeout": return "İstek zaman aşımına uğradı. Lütfen tekrar deneyin.";
    case "unauthorized": return "Oturumunuz geçersiz veya süresi dolmuş.";
    case "forbidden": return "Bu işlem için yetkiniz bulunmuyor.";
    case "not-found": return "İstenen kayıt bulunamadı.";
    case "conflict": return "İşlem mevcut durumla çakışıyor.";
    case "validation": return "Gönderilen bilgiler geçersiz.";
    case "rate-limit": return "Çok fazla istek gönderildi. Lütfen kısa bir süre sonra tekrar deneyin.";
    case "server": return "Sunucuda beklenmeyen bir hata oluştu.";
    case "unknown": return "Beklenmeyen bir hata oluştu.";
  }
}

export function normalizeApiError(error: unknown): ApiError {
  if (error instanceof ApiError) return error;

  if (!axios.isAxiosError(error)) {
    return new ApiError({ kind: "unknown", message: defaultMessage("unknown"), cause: error });
  }

  if (error.code === "ERR_CANCELED") {
    return new ApiError({ kind: "aborted", message: defaultMessage("aborted"), cause: error });
  }

  if (error.code === "ECONNABORTED" || error.code === "ETIMEDOUT") {
    return new ApiError({ kind: "timeout", message: defaultMessage("timeout"), cause: error });
  }

  if (!error.response) {
    return new ApiError({ kind: "network", message: defaultMessage("network"), cause: error });
  }

  const statusCode = error.response.status;
  const kind = classifyStatus(statusCode);
  const responseData: unknown = error.response.data;
  const backendMessage = isApiResult(responseData) || isApiResultWithoutData(responseData)
    ? responseData.message
    : null;
  const correlationId = error.response.headers["x-correlation-id"];

  return new ApiError({
    kind,
    statusCode,
    message: backendMessage?.trim() || defaultMessage(kind),
    ...(typeof correlationId === "string" ? { correlationId } : {}),
    cause: error,
  });
}
