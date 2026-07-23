import type { AxiosProgressEvent } from "axios";

import { apiClient } from "@/shared/api/axios";
import type { ApiResult } from "@/shared/api/api-result";
import { unwrapApiResult } from "@/shared/api/unwrap-api-result";
import type { UploadFileOptions, UploadProgress } from "@/shared/upload/upload-types";

function toProgress(event: AxiosProgressEvent): UploadProgress {
  const totalBytes = typeof event.total === "number" && event.total > 0 ? event.total : null;
  const percentage = totalBytes === null
    ? null
    : Math.min(100, Math.round((event.loaded / totalBytes) * 100));

  return {
    loadedBytes: event.loaded,
    totalBytes,
    percentage,
  };
}

export function createClientUploadId(): string {
  return crypto.randomUUID();
}

export async function uploadFile<TResponse>({
  url,
  file,
  fieldName = "file",
  clientUploadId = createClientUploadId(),
  headers,
  signal,
  onProgress,
}: UploadFileOptions): Promise<TResponse> {
  const formData = new FormData();
  formData.append(fieldName, file, file.name);

  const response = await apiClient.post<ApiResult<TResponse>>(url, formData, {
    signal,
    headers: {
      ...headers,
      "X-Client-Upload-Id": clientUploadId,
    },
    onUploadProgress: onProgress ? (event) => onProgress(toProgress(event)) : undefined,
  });

  return unwrapApiResult(response.data);
}
