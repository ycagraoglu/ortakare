import { useCallback, useEffect, useState } from "react";

interface ServiceWorkerState {
  isSupported: boolean;
  isOffline: boolean;
  updateAvailable: boolean;
}

export function useServiceWorker() {
  const [registration, setRegistration] = useState<ServiceWorkerRegistration | null>(null);
  const [state, setState] = useState<ServiceWorkerState>({
    isSupported: "serviceWorker" in navigator,
    isOffline: !navigator.onLine,
    updateAvailable: false,
  });

  useEffect(() => {
    const handleOnline = () => setState((current) => ({ ...current, isOffline: false }));
    const handleOffline = () => setState((current) => ({ ...current, isOffline: true }));

    window.addEventListener("online", handleOnline);
    window.addEventListener("offline", handleOffline);

    if (!("serviceWorker" in navigator) || import.meta.env.DEV) {
      return () => {
        window.removeEventListener("online", handleOnline);
        window.removeEventListener("offline", handleOffline);
      };
    }

    let active = true;
    let refreshing = false;

    navigator.serviceWorker.addEventListener("controllerchange", () => {
      if (refreshing) return;
      refreshing = true;
      window.location.reload();
    });

    void navigator.serviceWorker.register("/sw.js").then((nextRegistration) => {
      if (!active) return;
      setRegistration(nextRegistration);

      if (nextRegistration.waiting) {
        setState((current) => ({ ...current, updateAvailable: true }));
      }

      nextRegistration.addEventListener("updatefound", () => {
        const worker = nextRegistration.installing;
        if (!worker) return;

        worker.addEventListener("statechange", () => {
          if (worker.state === "installed" && navigator.serviceWorker.controller) {
            setState((current) => ({ ...current, updateAvailable: true }));
          }
        });
      });
    });

    return () => {
      active = false;
      window.removeEventListener("online", handleOnline);
      window.removeEventListener("offline", handleOffline);
    };
  }, []);

  const applyUpdate = useCallback(() => {
    registration?.waiting?.postMessage({ type: "SKIP_WAITING" });
  }, [registration]);

  return {
    ...state,
    applyUpdate,
  };
}
