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

export type ApiFieldErrors = Readonly<Record<string, readonly string[]>>;

interface ApiErrorOptions {
  kind: ApiErrorKind;
  message: string;
  statusCode?: number;
  correlationId?: string;
  fieldErrors?: ApiFieldErrors;
  cause?: unknown;
}

export class ApiError extends Error {
  readonly kind: ApiErrorKind;
  readonly statusCode: number | undefined;
  readonly correlationId: string | undefined;
  readonly fieldErrors: ApiFieldErrors;

  constructor(options: ApiErrorOptions) {
    super(options.message, { cause: options.cause });
    this.name = "ApiError";
    this.kind = options.kind;
    this.statusCode = options.statusCode;
    this.correlationId = options.correlationId;
    this.fieldErrors = options.fieldErrors ?? {};
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

function readFieldErrors(value: unknown): ApiFieldErrors {
  if (!value || typeof value !== "object") return {};

  const candidate = value as Record<string, unknown>;
  const source = candidate.errors ?? candidate.validationErrors;
  if (!source || typeof source !== "object" || Array.isArray(source)) return {};

  const result: Record<string, readonly string[]> = {};

  for (const [field, messages] of Object.entries(source as Record<string, unknown>)) {
    if (Array.isArray(messages)) {
      const normalized = messages.filter((message): message is string => typeof message === "string" && message.trim().length > 0);
      if (normalized.length > 0) result[field] = normalized;
      continue;
    }

    if (typeof messages === "string" && messages.trim().length > 0) {
      result[field] = [messages];
    }
  }

  return result;
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
  const fieldErrors = readFieldErrors(responseData);

  return new ApiError({
    kind,
    statusCode,
    message: backendMessage?.trim() || defaultMessage(kind),
    ...(typeof correlationId === "string" ? { correlationId } : {}),
    ...(Object.keys(fieldErrors).length > 0 ? { fieldErrors } : {}),
    cause: error,
  });
}
