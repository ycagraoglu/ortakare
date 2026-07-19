export interface ApiResult<T> {
  isSuccess: boolean;
  statusCode: number;
  message: string | null;
  data: T | null;
}

export interface ApiResultWithoutData {
  isSuccess: boolean;
  statusCode: number;
  message: string | null;
}

export function isApiResult(value: unknown): value is ApiResult<unknown> {
  if (typeof value !== "object" || value === null) {
    return false;
  }

  const candidate = value as Record<string, unknown>;

  return (
    typeof candidate.isSuccess === "boolean" &&
    typeof candidate.statusCode === "number" &&
    (typeof candidate.message === "string" || candidate.message === null) &&
    "data" in candidate
  );
}

export function isApiResultWithoutData(value: unknown): value is ApiResultWithoutData {
  if (typeof value !== "object" || value === null) {
    return false;
  }

  const candidate = value as Record<string, unknown>;

  return (
    typeof candidate.isSuccess === "boolean" &&
    typeof candidate.statusCode === "number" &&
    (typeof candidate.message === "string" || candidate.message === null)
  );
}
