import type { ReactNode } from "react";
import { Navigate, useLocation } from "react-router-dom";

import { useAuth } from "@/features/auth/hooks/use-auth";
import { RouteLoading } from "@/app/router/route-loading";

export function ProtectedRoute({ children }: { children: ReactNode }) {
  const { status } = useAuth();
  const location = useLocation();

  if (status === "initializing") return <RouteLoading />;
  if (status !== "authenticated") {
    return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  }

  return children;
}

export function AnonymousRoute({ children }: { children: ReactNode }) {
  const { status } = useAuth();

  if (status === "initializing") return <RouteLoading />;
  if (status === "authenticated") return <Navigate to="/dashboard" replace />;

  return children;
}

export function PermissionRoute({
  children,
  allowed,
}: {
  children: ReactNode;
  allowed: boolean;
}) {
  return allowed ? children : <Navigate to="/forbidden" replace />;
}
