import type { CSSProperties } from "react";

import "@/shared/ui/state/ui-state.css";

type SkeletonProps = {
  width?: CSSProperties["width"];
  height?: CSSProperties["height"];
  radius?: CSSProperties["borderRadius"];
  className?: string;
};

export function Skeleton({ width = "100%", height = 16, radius = 8, className }: SkeletonProps) {
  return (
    <span
      aria-hidden="true"
      className={["ui-skeleton", className].filter(Boolean).join(" ")}
      style={{ width, height, borderRadius: radius }}
    />
  );
}
