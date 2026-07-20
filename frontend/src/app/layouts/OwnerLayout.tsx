import { Outlet } from "react-router-dom";

import { OwnerSidebar } from "@/app/navigation/OwnerSidebar";
import { OwnerTopbar } from "@/app/navigation/OwnerTopbar";
import { RouteBreadcrumbs } from "@/app/router/route-breadcrumbs";
import "@/app/layouts/owner-layout.css";

export function OwnerLayout() {
  return (
    <div className="owner-shell">
      <OwnerSidebar />

      <div className="owner-shell__workspace">
        <OwnerTopbar />

        <main className="owner-shell__content" id="main-content">
          <RouteBreadcrumbs />
          <div className="owner-shell__page">
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  );
}
