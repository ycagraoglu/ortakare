import { isRouteErrorResponse, useRouteError } from "react-router-dom";

import { GlobalErrorFallback, classifyGlobalError, isChunkLoadError } from "@/shared/error";

const RELOAD_KEY = "ortakare.chunk-reload-attempted";

export function RouteErrorPage() {
  const error = useRouteError();

  if (isChunkLoadError(error) && sessionStorage.getItem(RELOAD_KEY) !== "true") {
    sessionStorage.setItem(RELOAD_KEY, "true");
    window.location.reload();

    return (
      <GlobalErrorFallback
        error={{
          kind: "chunk",
          title: "Yeni sürüm yükleniyor",
          description: "Uygulamanın güncel dosyaları alınıyor.",
        }}
      />
    );
  }

  sessionStorage.removeItem(RELOAD_KEY);

  if (isRouteErrorResponse(error)) {
    return (
      <GlobalErrorFallback
        error={{
          kind: error.status >= 500 ? "server" : "unknown",
          title: `${error.status} — Sayfa yüklenemedi`,
          description: error.statusText || "Bu sayfa görüntülenirken bir sorun oluştu.",
        }}
        onReload={() => window.location.reload()}
      />
    );
  }

  return (
    <GlobalErrorFallback
      error={classifyGlobalError(error)}
      onReload={() => window.location.reload()}
    />
  );
}
