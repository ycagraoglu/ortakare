import { useEffect } from "react";
import { useBlocker } from "react-router-dom";

export function useUnsavedChanges(isDirty: boolean, message = "Kaydedilmemiş değişiklikleriniz var. Sayfadan ayrılmak istiyor musunuz?") {
  const blocker = useBlocker(({ currentLocation, nextLocation }) =>
    isDirty && currentLocation.pathname !== nextLocation.pathname,
  );

  useEffect(() => {
    const handleBeforeUnload = (event: BeforeUnloadEvent) => {
      if (!isDirty) return;
      event.preventDefault();
      event.returnValue = "";
    };

    window.addEventListener("beforeunload", handleBeforeUnload);
    return () => window.removeEventListener("beforeunload", handleBeforeUnload);
  }, [isDirty]);

  useEffect(() => {
    if (blocker.state !== "blocked") return;

    if (window.confirm(message)) blocker.proceed();
    else blocker.reset();
  }, [blocker, message]);
}
