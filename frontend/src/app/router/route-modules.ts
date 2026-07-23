import { lazyRoute } from "@/app/router/lazy-route";

export const routeModules = {
  login: lazyRoute(() => import("@/features/auth/pages/LoginPage")),
  register: lazyRoute(() => import("@/features/auth/pages/RegisterPage")),
  dashboard: lazyRoute(() => import("@/features/dashboard/pages/DashboardPage")),
  events: lazyRoute(() => import("@/features/events/pages/EventsPage")),
  participants: lazyRoute(() => import("@/features/participants/pages/ParticipantsPage")),
  photos: lazyRoute(() => import("@/features/photos/pages/PhotosPage")),
  gallery: lazyRoute(() => import("@/features/gallery/pages/GalleryPage")),
  notifications: lazyRoute(() => import("@/features/notifications/pages/NotificationsPage")),
  settings: lazyRoute(() => import("@/features/settings/pages/SettingsPage")),
  forbidden: lazyRoute(() =>
    import("@/app/router/route-pages").then(({ ForbiddenPage }) => ({ default: ForbiddenPage })),
  ),
  notFound: lazyRoute(() =>
    import("@/app/router/route-pages").then(({ NotFoundPage }) => ({ default: NotFoundPage })),
  ),
  offline: lazyRoute(() =>
    import("@/app/router/route-pages").then(({ OfflinePage }) => ({ default: OfflinePage })),
  ),
} as const;

export const routePreloads = {
  dashboard: routeModules.dashboard.preload,
  events: routeModules.events.preload,
  participants: routeModules.participants.preload,
  photos: routeModules.photos.preload,
  gallery: routeModules.gallery.preload,
  notifications: routeModules.notifications.preload,
  settings: routeModules.settings.preload,
} as const;

export type OwnerRouteKey = keyof typeof routePreloads;
