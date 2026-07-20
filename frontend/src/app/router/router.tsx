import { Suspense, useEffect, type ReactNode } from "react";
import { Navigate, Outlet, createBrowserRouter, useMatches } from "react-router-dom";

import { OwnerLayout } from "@/app/layouts/OwnerLayout";
import { PublicLayout } from "@/app/layouts/PublicLayout";
import { lazyRoute } from "@/app/router/lazy-route";
import { AnonymousRoute, ProtectedRoute } from "@/app/router/route-guards";
import { RouteErrorPage } from "@/app/router/route-error";
import { RouteLoading } from "@/app/router/route-loading";
import type { AppRouteHandle } from "@/app/router/route-meta";

const LoginPage = lazyRoute(() => import("@/features/auth/pages/LoginPage"));
const RegisterPage = lazyRoute(() => import("@/features/auth/pages/RegisterPage"));
const DashboardPage = lazyRoute(() => import("@/features/dashboard/pages/DashboardPage"));
const EventsPage = lazyRoute(() => import("@/features/events/pages/EventsPage"));
const ParticipantsPage = lazyRoute(() => import("@/features/participants/pages/ParticipantsPage"));
const PhotosPage = lazyRoute(() => import("@/features/photos/pages/PhotosPage"));
const GalleryPage = lazyRoute(() => import("@/features/gallery/pages/GalleryPage"));
const NotificationsPage = lazyRoute(() => import("@/features/notifications/pages/NotificationsPage"));
const SettingsPage = lazyRoute(() => import("@/features/settings/pages/SettingsPage"));
const ForbiddenPage = lazyRoute(() =>
  import("@/app/router/route-pages").then(({ ForbiddenPage }) => ({ default: ForbiddenPage })),
);
const NotFoundPage = lazyRoute(() =>
  import("@/app/router/route-pages").then(({ NotFoundPage }) => ({ default: NotFoundPage })),
);
const OfflinePage = lazyRoute(() =>
  import("@/app/router/route-pages").then(({ OfflinePage }) => ({ default: OfflinePage })),
);

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

export const routePreloads = {
  dashboard: DashboardPage.preload,
  events: EventsPage.preload,
  participants: ParticipantsPage.preload,
  photos: PhotosPage.preload,
  gallery: GalleryPage.preload,
  notifications: NotificationsPage.preload,
  settings: SettingsPage.preload,
} as const;

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
