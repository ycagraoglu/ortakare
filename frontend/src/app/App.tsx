import { RouterProvider } from "react-router-dom";

import { router } from "@/app/router/router";
import { AuthBootstrap } from "@/features/auth/components/AuthBootstrap";

export function App() {
  return (
    <AuthBootstrap>
      <RouterProvider router={router} />
    </AuthBootstrap>
  );
}
