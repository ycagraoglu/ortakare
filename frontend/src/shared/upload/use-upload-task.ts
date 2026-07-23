import { useCallback, useRef, useState } from "react";

import { normalizeApiError } from "@/shared/api/api-error";
import { createClientUploadId } from "@/shared/upload/upload-file";
import type { UploadProgress, UploadStatus } from "@/shared/upload/upload-types";

const initialProgress: UploadProgress = {
  loadedBytes: 0,
  totalBytes: null,
  percentage: null,
};

export function useUploadTask<TResponse>() {
  const abortControllerRef = useRef<AbortController | null>(null);
  const clientUploadIdRef = useRef(createClientUploadId());
  const [status, setStatus] = useState<UploadStatus>("idle");
  const [progress, setProgress] = useState<UploadProgress>(initialProgress);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [response, setResponse] = useState<TResponse | null>(null);

  const reset = useCallback(() => {
    abortControllerRef.current?.abort();
    abortControllerRef.current = null;
    clientUploadIdRef.current = createClientUploadId();
    setStatus("idle");
    setProgress(initialProgress);
    setErrorMessage(null);
    setResponse(null);
  }, []);

  const cancel = useCallback(() => {
    abortControllerRef.current?.abort();
    abortControllerRef.current = null;
    setStatus("cancelled");
  }, []);

  const run = useCallback(async (
    operation: (options: {
      signal: AbortSignal;
      clientUploadId: string;
      onProgress: (progress: UploadProgress) => void;
    }) => Promise<TResponse>,
  ): Promise<TResponse | null> => {
    if (status === "uploading") return null;

    const controller = new AbortController();
    abortControllerRef.current = controller;
    setStatus("uploading");
    setProgress(initialProgress);
    setErrorMessage(null);

    try {
      const result = await operation({
        signal: controller.signal,
        clientUploadId: clientUploadIdRef.current,
        onProgress: setProgress,
      });
      setResponse(result);
      setStatus("success");
      return result;
    } catch (error) {
      const apiError = normalizeApiError(error);
      if (apiError.kind === "aborted") {
        setStatus("cancelled");
        return null;
      }

      setErrorMessage(apiError.message);
      setStatus("error");
      return null;
    } finally {
      abortControllerRef.current = null;
    }
  }, [status]);

  return {
    status,
    progress,
    errorMessage,
    response,
    clientUploadId: clientUploadIdRef.current,
    run,
    cancel,
    reset,
  };
}
