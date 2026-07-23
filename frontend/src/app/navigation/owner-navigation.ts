import { routePreloads, type OwnerRouteKey } from "@/app/router/route-modules";

export type OwnerNavigationItem = {
  key: OwnerRouteKey;
  to: `/${string}`;
  label: string;
  description: string;
  preload: () => Promise<unknown>;
  end?: boolean;
};

export const ownerNavigation: readonly OwnerNavigationItem[] = [
  {
    key: "dashboard",
    to: "/dashboard",
    label: "Dashboard",
    description: "Operasyon özeti",
    preload: routePreloads.dashboard,
    end: true,
  },
  {
    key: "events",
    to: "/events",
    label: "Etkinlikler",
    description: "Etkinlik yönetimi",
    preload: routePreloads.events,
  },
  {
    key: "participants",
    to: "/participants",
    label: "Katılımcılar",
    description: "Katılımcı kayıtları",
    preload: routePreloads.participants,
  },
  {
    key: "photos",
    to: "/photos",
    label: "Fotoğraflar",
    description: "Fotoğraf operasyonları",
    preload: routePreloads.photos,
  },
  {
    key: "gallery",
    to: "/gallery",
    label: "Galeri",
    description: "Galeri ve dışa aktarma",
    preload: routePreloads.gallery,
  },
  {
    key: "notifications",
    to: "/notifications",
    label: "Bildirimler",
    description: "Bildirim merkezi",
    preload: routePreloads.notifications,
  },
  {
    key: "settings",
    to: "/settings",
    label: "Ayarlar",
    description: "Hesap ve uygulama ayarları",
    preload: routePreloads.settings,
  },
] as const;
