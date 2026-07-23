import { Link, Outlet } from "react-router-dom";

import { RouteAccessibility } from "@/shared/accessibility/RouteAccessibility";
import { SkipLink } from "@/shared/accessibility/SkipLink";

export function PublicLayout() {
  return (
    <div>
      <SkipLink />
      <RouteAccessibility />
      <header>
        <Link to="/" aria-label="Ortakare ana sayfa">
          Ortakare
        </Link>
      </header>
      <main id="main-content" tabIndex={-1}>
        <Outlet />
      </main>
      <footer>© Ortakare</footer>
    </div>
  );
}
