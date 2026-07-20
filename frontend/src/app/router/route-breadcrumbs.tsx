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
    <nav className="route-breadcrumbs" aria-label="Sayfa yolu">
      <ol className="route-breadcrumbs__list">
        {items.map((item, index) => {
          const isCurrent = index === items.length - 1;

          return (
            <li className="route-breadcrumbs__item" key={`${item.pathname}-${item.label}`}>
              {isCurrent ? (
                <span aria-current="page">{item.label}</span>
              ) : (
                <Link to={item.pathname}>{item.label}</Link>
              )}
              {!isCurrent ? <span aria-hidden="true">/</span> : null}
            </li>
          );
        })}
      </ol>
    </nav>
  );
}
