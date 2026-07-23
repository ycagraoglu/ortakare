import { Link } from "react-router-dom";

import { FeaturePlaceholder } from "@/shared/ui/FeaturePlaceholder";

export function ForbiddenPage() {
  return (
    <FeaturePlaceholder
      title="403"
      description="Bu sayfayı görüntüleme yetkiniz bulunmuyor."
    />
  );
}

export function NotFoundPage() {
  return (
    <section aria-labelledby="page-title">
      <h1 id="page-title">404</h1>
      <p>Aradığınız sayfa bulunamadı.</p>
      <Link to="/">Ana sayfaya dön</Link>
    </section>
  );
}

export function OfflinePage() {
  return (
    <FeaturePlaceholder
      title="Çevrimdışı"
      description="İnternet bağlantınızı kontrol edip tekrar deneyin."
    />
  );
}
