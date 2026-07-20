import { Link } from "react-router-dom";

function FeaturePage({ title, description }: { title: string; description: string }) {
  return (
    <section>
      <h1>{title}</h1>
      <p>{description}</p>
    </section>
  );
}

export function LoginPage() {
  return (
    <section>
      <h1>Giriş</h1>
      <p>Giriş formu Form Standardı aşamasında bağlanacaktır.</p>
    </section>
  );
}

export function RegisterPage() {
  return <FeaturePage title="Kayıt" description="Kayıt formu sonraki aşamada bağlanacaktır." />;
}

export function DashboardPage() {
  return <FeaturePage title="Dashboard" description="Operasyon özeti burada yer alacaktır." />;
}

export function EventsPage() {
  return <FeaturePage title="Etkinlikler" description="Etkinlik listesi ve filtreleri burada yer alacaktır." />;
}

export function ParticipantsPage() {
  return <FeaturePage title="Katılımcılar" description="Katılımcı yönetimi burada yer alacaktır." />;
}

export function PhotosPage() {
  return <FeaturePage title="Fotoğraflar" description="Fotoğraf yönetimi burada yer alacaktır." />;
}

export function GalleryPage() {
  return <FeaturePage title="Galeri" description="Galeri ve dışa aktarım işlemleri burada yer alacaktır." />;
}

export function NotificationsPage() {
  return <FeaturePage title="Bildirimler" description="Bildirim merkezi burada yer alacaktır." />;
}

export function SettingsPage() {
  return <FeaturePage title="Ayarlar" description="Hesap ve uygulama ayarları burada yer alacaktır." />;
}

export function ForbiddenPage() {
  return <FeaturePage title="403" description="Bu sayfayı görüntüleme yetkiniz bulunmuyor." />;
}

export function NotFoundPage() {
  return (
    <section>
      <h1>404</h1>
      <p>Aradığınız sayfa bulunamadı.</p>
      <Link to="/">Ana sayfaya dön</Link>
    </section>
  );
}

export function OfflinePage() {
  return <FeaturePage title="Çevrimdışı" description="İnternet bağlantınızı kontrol edip tekrar deneyin." />;
}
