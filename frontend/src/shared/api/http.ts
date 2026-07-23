import type { AxiosRequestConfig } from "axios";

import { apiClient } from "@/shared/api/axios";
import type {
  ApiResult,
  ApiResultWithoutData,
} from "@/shared/api/api-result";
import {
  unwrapApiResult,
  unwrapApiResultWithoutData,
} from "@/shared/api/unwrap-api-result";

export interface RequestOptions {
  signal?: AbortSignal;
  headers?: Record<string, string>;
}

function createConfig(options?: RequestOptions): AxiosRequestConfig {
  if (!options) return {};

  return {
    ...(options.signal ? { signal: options.signal } : {}),
    ...(options.headers ? { headers: options.headers } : {}),
  };
}

export async function apiGet<T>(url: string, options?: RequestOptions): Promise<T> {
  const response = await apiClient.get<ApiResult<T>>(url, createConfig(options));
  return unwrapApiResult<T>(response.data);
}

export async function apiPost<TResponse, TRequest = unknown>(url: string, body?: TRequest, options?: RequestOptions): Promise<TResponse> {
  const response = await apiClient.post<ApiResult<TResponse>>(url, body, createConfig(options));
  return unwrapApiResult<TResponse>(response.data);
}

export async function apiPut<TResponse, TRequest = unknown>(url: string, body?: TRequest, options?: RequestOptions): Promise<TResponse> {
  const response = await apiClient.put<ApiResult<TResponse>>(url, body, createConfig(options));
  return unwrapApiResult<TResponse>(response.data);
}

export async function apiPatch<TResponse, TRequest = unknown>(url: string, body?: TRequest, options?: RequestOptions): Promise<TResponse> {
  const response = await apiClient.patch<ApiResult<TResponse>>(url, body, createConfig(options));
  return unwrapApiResult<TResponse>(response.data);
}

export async function apiDelete<TResponse>(url: string, options?: RequestOptions): Promise<TResponse> {
  const response = await apiClient.delete<ApiResult<TResponse>>(url, createConfig(options));
  return unwrapApiResult<TResponse>(response.data);
}

export async function apiPostWithoutData<TRequest = unknown>(url: string, body?: TRequest, options?: RequestOptions): Promise<void> {
  const response = await apiClient.post<ApiResultWithoutData>(url, body, createConfig(options));
  unwrapApiResultWithoutData(response.data);
}

export async function apiDeleteWithoutData(url: string, options?: RequestOptions): Promise<void> {
  const response = await apiClient.delete<ApiResultWithoutData>(url, createConfig(options));
  unwrapApiResultWithoutData(response.data);
}
