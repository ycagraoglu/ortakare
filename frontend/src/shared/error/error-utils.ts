import { ApiError } from "@/shared/api/api-error";

const CHUNK_ERROR_PATTERN = /ChunkLoadError|Failed to fetch dynamically imported module|Importing a module script failed/i;

export type GlobalErrorKind =
  | "chunk"
  | "offline"
  | "network"
  | "timeout"
  | "server"
  | "unknown";

export interface GlobalErrorDetails {
  kind: GlobalErrorKind;
  title: string;
  description: string;
  correlationId?: string;
}

export function isChunkLoadError(error: unknown): boolean {
  return error instanceof Error && CHUNK_ERROR_PATTERN.test(error.message);
}

export function classifyGlobalError(error: unknown): GlobalErrorDetails {
  if (isChunkLoadError(error)) {
    return {
      kind: "chunk",
      title: "Yeni sürüm yüklenemedi",
      description: "Uygulamanın güncel dosyaları alınamadı. Sayfayı yenileyerek tekrar deneyin.",
    };
  }

  if (typeof navigator !== "undefined" && !navigator.onLine) {
    return {
      kind: "offline",
      title: "İnternet bağlantısı yok",
      description: "Bağlantınızı kontrol ettikten sonra tekrar deneyin.",
    };
  }

  if (error instanceof ApiError) {
    if (error.kind === "network") {
      return {
        kind: "network",
        title: "Sunucuya ulaşılamıyor",
        description: error.message,
        correlationId: error.correlationId,
      };
    }

    if (error.kind === "timeout") {
      return {
        kind: "timeout",
        title: "İstek zaman aşımına uğradı",
        description: error.message,
        correlationId: error.correlationId,
      };
    }

    if (error.kind === "server") {
      return {
        kind: "server",
        title: "Sunucu hatası",
        description: error.message,
        correlationId: error.correlationId,
      };
    }
  }

  return {
    kind: "unknown",
    title: "Beklenmeyen bir hata oluştu",
    description: "Uygulama bu bölümü görüntülerken beklenmeyen bir sorunla karşılaştı.",
  };
}
