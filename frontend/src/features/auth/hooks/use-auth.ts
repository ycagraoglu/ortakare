import { useSyncExternalStore } from "react";

import { authSession } from "@/features/auth/model/auth-session";

export function useAuth() {
  const snapshot = useSyncExternalStore(
    authSession.subscribe,
    authSession.getSnapshot,
    authSession.getSnapshot,
  );

  return {
    ...snapshot,
    login: authSession.login,
    logout: authSession.logout,
  };
}
