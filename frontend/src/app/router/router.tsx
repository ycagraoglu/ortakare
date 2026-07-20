import { Suspense, useEffect, type ReactNode } from "react";
import { Navigate, Outlet, createBrowserRouter, useMatches } from "react-router-dom";

import { OwnerLayout } from "@/app/layouts/OwnerLayout";
import { PublicLayout } from "@/app/layouts/PublicLayout";
import { AnonymousRoute, ProtectedRoute } from "@/app/router/route-guards";
import { RouteErrorPage } from "@/app/router/route-error";
import { RouteLoading } from "@/app/router/route-loading";
import type { AppRouteHandle } from "@/app/router/route-meta";
import { routeModules } from "@/app/router/route-modules";

function LazyPage({ children }: { children: ReactNode }) {
  return <Suspense fallback={<RouteLoading />}>{children}</Suspense>;
}

function RouteTitle() {
  const matches = useMatches();

  useEffect(() => {
    const title = [...matches]
      .reverse()
      .map((match) => (match.handle as AppRouteHandle | undefined)?.meta.title)
      .find(Boolean);

    document.title = title ? `${title} | Ortakare` : "Ortakare";
  }, [matches]);

  return <Outlet />;
}

const meta = (title: string, breadcrumb?: string): AppRouteHandle => ({
  meta: breadcrumb ? { title, breadcrumb } : { title },
});

const {
  login: LoginPage,
  register: RegisterPage,
  dashboard: DashboardPage,
  events: EventsPage,
  participants: ParticipantsPage,
  photos: PhotosPage,
  gallery: GalleryPage,
  notifications: NotificationsPage,
  settings: SettingsPage,
  forbidden: ForbiddenPage,
  notFound: NotFoundPage,
  offline: OfflinePage,
} = routeModules;

export const router = createBrowserRouter([
  {
    element: <RouteTitle />,
    errorElement: <RouteErrorPage />,
    children: [
      {
        element: <PublicLayout />,
        children: [
          { index: true, element: <Navigate to="/login" replace /> },
          {
            path: "login",
            handle: meta("Giriş"),
            element: <AnonymousRoute><LazyPage><LoginPage /></LazyPage></AnonymousRoute>,
          },
          {
            path: "register",
            handle: meta("Kayıt"),
            element: <AnonymousRoute><LazyPage><RegisterPage /></LazyPage></AnonymousRoute>,
          },
          { path: "forbidden", handle: meta("Yetkisiz Erişim"), element: <LazyPage><ForbiddenPage /></LazyPage> },
          { path: "offline", handle: meta("Çevrimdışı"), element: <LazyPage><OfflinePage /></LazyPage> },
        ],
      },
      {
        element: <ProtectedRoute><OwnerLayout /></ProtectedRoute>,
        handle: meta("Ortakare", "Ana Sayfa"),
        children: [
          { path: "dashboard", handle: meta("Dashboard", "Dashboard"), element: <LazyPage><DashboardPage /></LazyPage> },
          { path: "events", handle: meta("Etkinlikler", "Etkinlikler"), element: <LazyPage><EventsPage /></LazyPage> },
          { path: "participants", handle: meta("Katılımcılar", "Katılımcılar"), element: <LazyPage><ParticipantsPage /></LazyPage> },
          { path: "photos", handle: meta("Fotoğraflar", "Fotoğraflar"), element: <LazyPage><PhotosPage /></LazyPage> },
          { path: "gallery", handle: meta("Galeri", "Galeri"), element: <LazyPage><GalleryPage /></LazyPage> },
          { path: "notifications", handle: meta("Bildirimler", "Bildirimler"), element: <LazyPage><NotificationsPage /></LazyPage> },
          { path: "settings", handle: meta("Ayarlar", "Ayarlar"), element: <LazyPage><SettingsPage /></LazyPage> },
        ],
      },
      { path: "*", handle: meta("Sayfa Bulunamadı"), element: <LazyPage><NotFoundPage /></LazyPage> },
    ],
  },
]);
