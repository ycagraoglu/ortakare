import { EmptyState } from "@/shared/ui/state";

export default function EventsPage() {
  return (
    <section aria-labelledby="events-page-title">
      <header>
        <h1 id="events-page-title">Etkinlikler</h1>
        <p>Etkinliklerinizi oluşturun, planlayın ve yönetin.</p>
      </header>

      <EmptyState
        title="Henüz etkinlik bulunmuyor"
        description="İlk etkinliğinizi oluşturduğunuzda yaklaşan ve geçmiş etkinlikler burada listelenecek."
        action={(
          <button type="button" className="ui-state-button" disabled aria-disabled="true">
            Etkinlik oluşturma yakında
          </button>
        )}
      />
    </section>
  );
}
