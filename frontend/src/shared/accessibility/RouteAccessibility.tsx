import { useEffect, useRef } from "react";
import { useLocation, useMatches } from "react-router-dom";

import type { AppRouteHandle } from "@/app/router/route-meta";
import "@/shared/accessibility/accessibility.css";

const APP_NAME = "Ortakare";

function getRouteTitle(matches: ReturnType<typeof useMatches>): string {
  for (let index = matches.length - 1; index >= 0; index -= 1) {
    const handle = matches[index].handle as AppRouteHandle | undefined;
    if (handle?.meta.title) return handle.meta.title;
  }

  return APP_NAME;
}

export function RouteAccessibility() {
  const location = useLocation();
  const matches = useMatches();
  const announcementRef = useRef<HTMLDivElement>(null);
  const isInitialRender = useRef(true);
  const routeTitle = getRouteTitle(matches);

  useEffect(() => {
    document.title = routeTitle === APP_NAME ? APP_NAME : `${routeTitle} | ${APP_NAME}`;

    if (announcementRef.current) {
      announcementRef.current.textContent = `${routeTitle} sayfası açıldı.`;
    }

    if (isInitialRender.current) {
      isInitialRender.current = false;
      return;
    }

    window.requestAnimationFrame(() => {
      const main = document.getElementById("main-content");
      main?.focus({ preventScroll: true });
      main?.scrollIntoView({ block: "start" });
    });
  }, [location.key, routeTitle]);

  return (
    <div
      ref={announcementRef}
      className="sr-only"
      aria-live="polite"
      aria-atomic="true"
    />
  );
}
