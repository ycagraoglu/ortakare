import type { GlobalErrorDetails } from "@/shared/error/error-utils";

interface GlobalErrorFallbackProps {
  error: GlobalErrorDetails;
  onRetry?: () => void;
  onReload?: () => void;
}

export function GlobalErrorFallback({ error, onRetry, onReload }: GlobalErrorFallbackProps) {
  return (
    <main className="global-error" role="alert" aria-labelledby="global-error-title">
      <section className="global-error__panel">
        <p className="global-error__eyebrow">Ortakare</p>
        <h1 id="global-error-title">{error.title}</h1>
        <p>{error.description}</p>

        {error.correlationId ? (
          <p className="global-error__correlation">
            Destek kodu: <code>{error.correlationId}</code>
          </p>
        ) : null}

        <div className="global-error__actions">
          {onRetry ? (
            <button type="button" onClick={onRetry}>
              Tekrar dene
            </button>
          ) : null}
          {onReload ? (
            <button type="button" onClick={onReload}>
              Sayfayı yenile
            </button>
          ) : null}
        </div>
      </section>
    </main>
  );
}
