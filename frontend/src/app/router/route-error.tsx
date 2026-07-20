import { isRouteErrorResponse, useRouteError } from "react-router-dom";

const RELOAD_KEY = "ortakare.chunk-reload-attempted";

function isChunkLoadError(error: unknown): boolean {
  return error instanceof Error && /ChunkLoadError|Failed to fetch dynamically imported module|Importing a module script failed/i.test(error.message);
}

export function RouteErrorPage() {
  const error = useRouteError();

  if (isChunkLoadError(error) && sessionStorage.getItem(RELOAD_KEY) !== "true") {
    sessionStorage.setItem(RELOAD_KEY, "true");
    window.location.reload();
    return <p role="status">Yeni sürüm yükleniyor…</p>;
  }

  sessionStorage.removeItem(RELOAD_KEY);

  if (isRouteErrorResponse(error)) {
    return (
      <section>
        <h1>{error.status}</h1>
        <p>{error.statusText || "Sayfa yüklenemedi."}</p>
      </section>
    );
  }

  return (
    <section>
      <h1>Beklenmeyen hata</h1>
      <p>Bu bölüm yüklenirken bir sorun oluştu.</p>
      <button type="button" onClick={() => window.location.reload()}>Tekrar dene</button>
    </section>
  );
}
