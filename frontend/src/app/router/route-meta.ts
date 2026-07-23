export interface RouteMeta {
  title: string;
  breadcrumb?: string;
  permission?: string;
}

export interface AppRouteHandle {
  meta: RouteMeta;
}
