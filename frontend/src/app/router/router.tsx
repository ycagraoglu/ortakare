import { Suspense, lazy, useEffect } from "react";
import { Navigate, Outlet, createBrowserRouter, useMatches } from "react-router-dom";

import { OwnerLayout } from "@/app/layouts/OwnerLayout";
import { PublicLayout } from "@/app/layouts/PublicLayout";
import { AnonymousRoute, ProtectedRoute } from "@/app/router/route-guards";
import { RouteErrorPage } from "@/app/router/route-error";
import { RouteLoading } from "@/app/router/route-loading";
import type { AppRouteHandle } from "@/app/router/route-meta";

const LoginPage = lazy(() => import("@/app/router/route-pages").then((module) => ({ default: module.LoginPage })));
const RegisterPage = lazy(() => import("@/app/router/route-pages").then((module) => ({ default: module.RegisterPage })));
const DashboardPage = lazy(() => import("@/app/router/route-pages").then((module) => ({ default: module.DashboardPage })));
const EventsPage = lazy(() => import("@/app/router/route-pages").then((module) => ({ default: module.EventsPage })));
const ParticipantsPage = lazy(() => import("@/app/router/route-pages").then((module) => ({ default: module.ParticipantsPage })));
const PhotosPage = lazy(() => import("@/app/router/route-pages").then((module) => ({ default: module.PhotosPage })));
const GalleryPage = lazy(() => import("@/app/router/route-pages").then((module) => ({ default: module.GalleryPage })));
const NotificationsPage = lazy(() => import("@/app/router/route-pages").then((module) => ({ default: module.NotificationsPage })));
const SettingsPage = lazy(() => import("@/app/router/route-pages").then((module) => ({ default: module.SettingsPage })));
const ForbiddenPage = lazy(() => import("@/app/router/route-pages").then((module) => ({ default: module.ForbiddenPage })));
const NotFoundPage = lazy(() => import("@/app/router/route-pages").then((module) => ({ default: module.NotFoundPage })));
const OfflinePage = lazy(() => import("@/app/router/route-pages").then((module) => ({ default: module.OfflinePage })));

function LazyPage({ children }: { children: React.ReactNode }) {
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
