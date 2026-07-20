import { NavLink, Outlet } from "react-router-dom";

import { RouteBreadcrumbs } from "@/app/router/route-breadcrumbs";
import { useAuth } from "@/features/auth/hooks/use-auth";

const navigation = [
  ["/dashboard", "Dashboard"],
  ["/events", "Etkinlikler"],
  ["/participants", "Katılımcılar"],
  ["/photos", "Fotoğraflar"],
  ["/gallery", "Galeri"],
  ["/notifications", "Bildirimler"],
  ["/settings", "Ayarlar"],
] as const;

export function OwnerLayout() {
  const { user, logout } = useAuth();

  return (
    <div style={{ display: "grid", gridTemplateColumns: "240px 1fr", minHeight: "100vh" }}>
      <aside>
        <strong>Ortakare</strong>
        <nav aria-label="Ana menü">
          {navigation.map(([to, label]) => (
            <div key={to}>
              <NavLink to={to}>{label}</NavLink>
            </div>
          ))}
        </nav>
      </aside>
      <div>
        <header>
          <span>{user?.displayName}</span>{" "}
          <button type="button" onClick={() => void logout()}>Çıkış</button>
        </header>
        <main>
          <RouteBreadcrumbs />
          <Outlet />
        </main>
      </div>
    </div>
  );
}
