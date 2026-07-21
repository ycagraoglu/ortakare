import { RouterProvider } from "react-router-dom";

import { router } from "@/app/router/router";
import { AuthBootstrap } from "@/features/auth/components/AuthBootstrap";
import { ApplicationErrorBoundary } from "@/shared/error";
import { PwaStatus } from "@/shared/pwa/PwaStatus";

export function App() {
  return (
    <ApplicationErrorBoundary>
      <AuthBootstrap>
        <RouterProvider router={router} />
        <PwaStatus />
      </AuthBootstrap>
    </ApplicationErrorBoundary>
  );
}
