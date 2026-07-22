import { Outlet } from "react-router-dom";

import { OwnerSidebar } from "@/app/navigation/OwnerSidebar";
import { OwnerTopbar } from "@/app/navigation/OwnerTopbar";
import { RouteBreadcrumbs } from "@/app/router/route-breadcrumbs";
import { RouteAccessibility } from "@/shared/accessibility/RouteAccessibility";
import { SkipLink } from "@/shared/accessibility/SkipLink";
import "@/app/layouts/owner-layout.css";

export function OwnerLayout() {
  return (
    <div className="owner-shell">
      <SkipLink />
      <RouteAccessibility />
      <OwnerSidebar />

      <div className="owner-shell__workspace">
        <OwnerTopbar />

        <main
          className="owner-shell__content"
          id="main-content"
          tabIndex={-1}
        >
          <RouteBreadcrumbs />
          <div className="owner-shell__page">
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  );
}
