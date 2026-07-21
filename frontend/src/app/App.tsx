import { RouterProvider } from "react-router-dom";

import { router } from "@/app/router/router";
import { AuthBootstrap } from "@/features/auth/components/AuthBootstrap";
import { ApplicationErrorBoundary } from "@/shared/error";

export function App() {
  return (
    <ApplicationErrorBoundary>
      <AuthBootstrap>
        <RouterProvider router={router} />
      </AuthBootstrap>
    </ApplicationErrorBoundary>
  );
}
