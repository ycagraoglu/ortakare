import { env } from "@/shared/config/env";

export function App() {
  return (
    <main>
      <h1>Ortakare</h1>
      <p>Frontend production foundation hazır.</p>
      <small>API: {env.VITE_API_URL}</small>
    </main>
  );
}
