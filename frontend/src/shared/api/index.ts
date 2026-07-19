export { ApiError, normalizeApiError, type ApiErrorKind } from "@/shared/api/api-error";
export type { ApiResult, ApiResultWithoutData } from "@/shared/api/api-result";
export { configureApiAuthentication } from "@/shared/api/auth-bridge";
export {
  apiDelete,
  apiDeleteWithoutData,
  apiGet,
  apiPatch,
  apiPost,
  apiPostWithoutData,
  apiPut,
  type RequestOptions,
} from "@/shared/api/http";
