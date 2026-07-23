import { type PropsWithChildren, useEffect, useRef } from "react";

import { useAuth } from "@/features/auth/hooks/use-auth";
import { authSession } from "@/features/auth/model/auth-session";

export function AuthBootstrap({ children }: PropsWithChildren) {
  const { status } = useAuth();
  const restoreStarted = useRef(false);

  useEffect(() => {
    if (restoreStarted.current) return;
    restoreStarted.current = true;
    void authSession.restore();
  }, []);

  if (status === "initializing") {
    return <p role="status">Oturum kontrol ediliyor…</p>;
  }

  return children;
}
