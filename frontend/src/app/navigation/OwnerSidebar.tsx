import { NavLink } from "react-router-dom";

import { ownerNavigation } from "@/app/navigation/owner-navigation";

function preloadSafely(preload: () => Promise<unknown>): void {
  void preload().catch(() => undefined);
}

export function OwnerSidebar() {
  return (
    <aside className="owner-sidebar" aria-label="Uygulama menüsü">
      <div className="owner-sidebar__brand">
        <span className="owner-sidebar__brand-mark" aria-hidden="true">O</span>
        <div>
          <strong>Ortakare</strong>
          <small>Stüdyo yönetimi</small>
        </div>
      </div>

      <nav aria-label="Ana menü">
        <ul className="owner-sidebar__list">
          {ownerNavigation.map((item) => (
            <li key={item.key}>
              <NavLink
                to={item.to}
                end={item.end}
                className={({ isActive }) =>
                  isActive
                    ? "owner-sidebar__link owner-sidebar__link--active"
                    : "owner-sidebar__link"
                }
                onMouseEnter={() => preloadSafely(item.preload)}
                onFocus={() => preloadSafely(item.preload)}
              >
                <span className="owner-sidebar__link-label">{item.label}</span>
                <small>{item.description}</small>
              </NavLink>
            </li>
          ))}
        </ul>
      </nav>
    </aside>
  );
}
