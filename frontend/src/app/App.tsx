import { AuthBootstrap } from "@/features/auth/components/AuthBootstrap";
import { env } from "@/shared/config/env";

export function App() {
  return (
    <AuthBootstrap>
      <main>
        <h1>Ortakare</h1>
        <p>Frontend production foundation hazır.</p>
        <small>API: {env.VITE_API_URL}</small>
      </main>
    </AuthBootstrap>
  );
}
