import { Link, useMatches } from "react-router-dom";

import type { AppRouteHandle } from "@/app/router/route-meta";

export function RouteBreadcrumbs() {
  const matches = useMatches();
  const items = matches.flatMap((match) => {
    const handle = match.handle as AppRouteHandle | undefined;
    const label = handle?.meta.breadcrumb;
    return label ? [{ label, pathname: match.pathname }] : [];
  });

  if (items.length === 0) return null;

  return (
    <nav aria-label="Sayfa yolu">
      <ol>
        {items.map((item, index) => (
          <li key={`${item.pathname}-${item.label}`} style={{ display: "inline" }}>
            {index < items.length - 1 ? <Link to={item.pathname}>{item.label}</Link> : <span aria-current="page">{item.label}</span>}
            {index < items.length - 1 ? " / " : null}
          </li>
        ))}
      </ol>
    </nav>
  );
}
