import type { ReactNode } from "react";

import { ErrorState } from "@/shared/ui/state/StatePanel";
import { LoadingState } from "@/shared/ui/state/LoadingState";

type AsyncStateProps = {
  isLoading: boolean;
  error?: string | null;
  onRetry?: () => void;
  loadingLabel?: string;
  children: ReactNode;
};

export function AsyncState({
  isLoading,
  error,
  onRetry,
  loadingLabel,
  children,
}: AsyncStateProps) {
  if (isLoading) {
    return <LoadingState label={loadingLabel} />;
  }

  if (error) {
    return (
      <ErrorState
        title="İçerik yüklenemedi"
        description={error}
        action={onRetry ? (
          <button type="button" className="ui-state-button" onClick={onRetry}>
            Tekrar dene
          </button>
        ) : undefined}
      />
    );
  }

  return children;
}
