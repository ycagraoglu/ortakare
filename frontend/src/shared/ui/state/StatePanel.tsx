import type { ReactNode } from "react";

import "@/shared/ui/state/ui-state.css";

type StatePanelTone = "neutral" | "danger";

type StatePanelProps = {
  title: string;
  description: string;
  action?: ReactNode;
  icon?: ReactNode;
  tone?: StatePanelTone;
  role?: "status" | "alert";
};

export function StatePanel({
  title,
  description,
  action,
  icon,
  tone = "neutral",
  role = "status",
}: StatePanelProps) {
  return (
    <section className={`ui-state-panel ui-state-panel--${tone}`} role={role} aria-live="polite">
      {icon ? <div className="ui-state-panel__icon" aria-hidden="true">{icon}</div> : null}
      <h2 className="ui-state-panel__title">{title}</h2>
      <p className="ui-state-panel__description">{description}</p>
      {action ? <div className="ui-state-panel__action">{action}</div> : null}
    </section>
  );
}

export function EmptyState(props: Omit<StatePanelProps, "tone" | "role">) {
  return <StatePanel {...props} tone="neutral" role="status" />;
}

export function ErrorState(props: Omit<StatePanelProps, "tone" | "role">) {
  return <StatePanel {...props} tone="danger" role="alert" />;
}
