import type { FileValidationResult, ImageUploadPolicy } from "@/shared/upload/upload-types";
import { formatFileSize } from "@/shared/upload/image-upload-policy";

function hasUnsafeFileName(fileName: string): boolean {
  return fileName.length === 0 || [...fileName].some((character) => character.charCodeAt(0) < 32);
}

export function validateImageFile(file: File, policy: ImageUploadPolicy): FileValidationResult {
  if (file.size <= 0) {
    return { isValid: false, message: "Boş dosyalar yüklenemez." };
  }

  if (file.size > policy.maxFileSizeBytes) {
    return {
      isValid: false,
      message: `Dosya boyutu en fazla ${formatFileSize(policy.maxFileSizeBytes)} olabilir.`,
    };
  }

  if (!policy.allowedMimeTypes.includes(file.type)) {
    return {
      isValid: false,
      message: "Yalnızca JPEG, PNG veya WebP görselleri yüklenebilir.",
    };
  }

  if (file.name.length > policy.maxFileNameLength || hasUnsafeFileName(file.name)) {
    return { isValid: false, message: "Dosya adı geçersiz veya çok uzun." };
  }

  return { isValid: true };
}
