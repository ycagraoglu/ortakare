import { ApiError } from "@/shared/api/api-error";
import {
  isApiResult,
  isApiResultWithoutData,
  type ApiResult,
  type ApiResultWithoutData,
} from "@/shared/api/api-result";

export function unwrapApiResult<T>(payload: ApiResult<T> | unknown): T {
  if (!isApiResult(payload)) {
    throw new ApiError({
      kind: "unknown",
      message: "Sunucudan beklenmeyen bir yanıt alındı.",
    });
  }

  if (!payload.isSuccess) {
    throw new ApiError({
      kind: "unknown",
      statusCode: payload.statusCode,
      message: payload.message?.trim() || "İşlem tamamlanamadı.",
    });
  }

  if (payload.data === null || payload.data === undefined) {
    throw new ApiError({
      kind: "unknown",
      statusCode: payload.statusCode,
      message: "Sunucu başarılı yanıt verdi ancak veri döndürmedi.",
    });
  }

  return payload.data as T;
}

export function unwrapApiResultWithoutData(
  payload: ApiResultWithoutData | unknown,
): void {
  if (!isApiResultWithoutData(payload)) {
    throw new ApiError({
      kind: "unknown",
      message: "Sunucudan beklenmeyen bir yanıt alındı.",
    });
  }

  if (!payload.isSuccess) {
    throw new ApiError({
      kind: "unknown",
      statusCode: payload.statusCode,
      message: payload.message?.trim() || "İşlem tamamlanamadı.",
    });
  }
}
