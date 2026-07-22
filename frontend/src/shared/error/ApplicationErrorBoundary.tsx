import { Component, type ErrorInfo, type ReactNode } from "react";

import { GlobalErrorFallback } from "@/shared/error/GlobalErrorFallback";
import { classifyGlobalError, type GlobalErrorDetails } from "@/shared/error/error-utils";
import { reportClientError } from "@/shared/observability";

interface ApplicationErrorBoundaryProps {
  children: ReactNode;
}

interface ApplicationErrorBoundaryState {
  error: GlobalErrorDetails | null;
}

export class ApplicationErrorBoundary extends Component<
  ApplicationErrorBoundaryProps,
  ApplicationErrorBoundaryState
> {
  state: ApplicationErrorBoundaryState = { error: null };

  static getDerivedStateFromError(error: unknown): ApplicationErrorBoundaryState {
    return { error: classifyGlobalError(error) };
  }

  componentDidCatch(error: unknown, info: ErrorInfo): void {
    reportClientError(error, { source: "error-boundary" });

    if (import.meta.env.DEV) {
      console.error("Unhandled application error", error, info);
    }
  }

  private reset = (): void => {
    this.setState({ error: null });
  };

  private reload = (): void => {
    window.location.reload();
  };

  render() {
    if (this.state.error) {
      return (
        <GlobalErrorFallback
          error={this.state.error}
          onRetry={this.reset}
          onReload={this.reload}
        />
      );
    }

    return this.props.children;
  }
}
