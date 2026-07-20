import { Link, Outlet } from "react-router-dom";

export function PublicLayout() {
  return (
    <div>
      <header>
        <Link to="/">Ortakare</Link>
      </header>
      <main>
        <Outlet />
      </main>
      <footer>© Ortakare</footer>
    </div>
  );
}
