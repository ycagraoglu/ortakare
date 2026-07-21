import { Skeleton } from "@/shared/ui/state/Skeleton";

import "@/shared/ui/state/ui-state.css";

type LoadingStateProps = {
  label?: string;
  rows?: number;
};

export function LoadingState({ label = "İçerik yükleniyor", rows = 3 }: LoadingStateProps) {
  return (
    <div className="ui-loading-state" role="status" aria-live="polite" aria-label={label}>
      <Skeleton width="38%" height={28} />
      <Skeleton width="68%" height={16} />
      <div className="ui-loading-state__rows">
        {Array.from({ length: rows }, (_, index) => (
          <Skeleton key={index} height={72} radius={12} />
        ))}
      </div>
      <span className="ui-visually-hidden">{label}</span>
    </div>
  );
}
