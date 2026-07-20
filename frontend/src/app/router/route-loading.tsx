export function RouteLoading() {
  return (
    <div role="status" aria-live="polite" aria-label="Sayfa yükleniyor">
      <div style={{ height: 28, width: "40%", background: "#e5e7eb", borderRadius: 8 }} />
      <div style={{ height: 16, width: "70%", background: "#e5e7eb", borderRadius: 8, marginTop: 16 }} />
      <div style={{ height: 160, width: "100%", background: "#e5e7eb", borderRadius: 12, marginTop: 24 }} />
    </div>
  );
}
