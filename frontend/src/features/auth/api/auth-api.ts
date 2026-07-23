import { apiClient } from "@/shared/api/axios";
import type { ApiResult, ApiResultWithoutData } from "@/shared/api/api-result";
import { unwrapApiResult, unwrapApiResultWithoutData } from "@/shared/api/unwrap-api-result";
import type {
  LoginRequest,
  LoginResponse,
  RefreshResponse,
} from "@/features/auth/model/auth-types";

export async function login(request: LoginRequest): Promise<LoginResponse> {
  const response = await apiClient.post<ApiResult<LoginResponse>>(
    "/api/auth/login",
    request,
    { skipAuthRefresh: true },
  );

  return unwrapApiResult(response.data);
}

export async function refresh(refreshToken: string): Promise<RefreshResponse> {
  const response = await apiClient.post<ApiResult<RefreshResponse>>(
    "/api/auth/refresh",
    { refreshToken },
    { skipAuthRefresh: true },
  );

  return unwrapApiResult(response.data);
}

export async function logout(refreshToken: string): Promise<void> {
  const response = await apiClient.post<ApiResultWithoutData>(
    "/api/auth/logout",
    { refreshToken },
    { skipAuthRefresh: true },
  );

  unwrapApiResultWithoutData(response.data);
}
