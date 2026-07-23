export type UploadStatus =
  | "idle"
  | "validating"
  | "ready"
  | "uploading"
  | "success"
  | "cancelled"
  | "error";

export type UploadProgress = {
  loadedBytes: number;
  totalBytes: number | null;
  percentage: number | null;
};

export type ImageUploadPolicy = {
  allowedMimeTypes: readonly string[];
  maxFileSizeBytes: number;
  maxFileNameLength: number;
};

export type FileValidationResult =
  | { isValid: true }
  | { isValid: false; message: string };

export type UploadFileOptions = {
  url: string;
  file: File;
  fieldName?: string;
  clientUploadId?: string;
  headers?: Record<string, string>;
  signal?: AbortSignal;
  onProgress?: (progress: UploadProgress) => void;
};
