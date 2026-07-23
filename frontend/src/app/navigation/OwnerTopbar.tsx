import { useAuth } from "@/features/auth/hooks/use-auth";

export function OwnerTopbar() {
  const { user, logout } = useAuth();

  return (
    <header className="owner-topbar">
      <div>
        <p className="owner-topbar__eyebrow">Ortakare Yönetim</p>
        <strong>{user?.displayName ?? "Kullanıcı"}</strong>
      </div>

      <button
        type="button"
        className="owner-topbar__logout"
        onClick={() => void logout()}
      >
        Çıkış
      </button>
    </header>
  );
}
