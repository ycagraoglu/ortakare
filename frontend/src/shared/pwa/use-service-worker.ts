import { useCallback, useEffect, useState } from "react";

interface ServiceWorkerState {
  isSupported: boolean;
  isOffline: boolean;
  updateAvailable: boolean;
}

const UPDATE_CHECK_INTERVAL_MS = 60 * 60 * 1000;

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
    let refreshing = false;
    const handleControllerChange = () => {
      if (refreshing) return;
      refreshing = true;
      window.location.reload();
    };

    window.addEventListener("online", handleOnline);
    window.addEventListener("offline", handleOffline);

    if (!("serviceWorker" in navigator) || import.meta.env.DEV) {
      return () => {
        window.removeEventListener("online", handleOnline);
        window.removeEventListener("offline", handleOffline);
      };
    }

    let active = true;
    let updateTimer: number | undefined;

    navigator.serviceWorker.addEventListener("controllerchange", handleControllerChange);

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

      updateTimer = window.setInterval(() => {
        if (navigator.onLine) void nextRegistration.update();
      }, UPDATE_CHECK_INTERVAL_MS);
    }).catch((error: unknown) => {
      if (import.meta.env.DEV) console.error("Service worker registration failed", error);
    });

    return () => {
      active = false;
      if (updateTimer !== undefined) window.clearInterval(updateTimer);
      window.removeEventListener("online", handleOnline);
      window.removeEventListener("offline", handleOffline);
      navigator.serviceWorker.removeEventListener("controllerchange", handleControllerChange);
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
