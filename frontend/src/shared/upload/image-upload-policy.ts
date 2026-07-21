import type { ImageUploadPolicy } from "@/shared/upload/upload-types";

export const defaultImageUploadPolicy: ImageUploadPolicy = {
  allowedMimeTypes: ["image/jpeg", "image/png", "image/webp"],
  maxFileSizeBytes: 15 * 1024 * 1024,
  maxFileNameLength: 180,
};

export function formatFileSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}
