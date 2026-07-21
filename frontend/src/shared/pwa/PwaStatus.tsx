import { useServiceWorker } from "@/shared/pwa/use-service-worker";
import "@/shared/pwa/pwa.css";

export function PwaStatus() {
  const { isOffline, updateAvailable, applyUpdate } = useServiceWorker();

  if (!isOffline && !updateAvailable) return null;

  return (
    <div className="pwa-status" aria-live="polite">
      {isOffline ? (
        <div className="pwa-status__notice" role="status">
          <strong>Çevrimdışısınız.</strong>
          <span>Önceden yüklenmiş ekranlar açılabilir; gönderme ve güncelleme işlemleri bağlantı gerektirir.</span>
        </div>
      ) : null}

      {updateAvailable ? (
        <div className="pwa-status__notice" role="status">
          <span>Ortakare'nin yeni bir sürümü hazır.</span>
          <button type="button" onClick={applyUpdate}>Şimdi güncelle</button>
        </div>
      ) : null}
    </div>
  );
}
