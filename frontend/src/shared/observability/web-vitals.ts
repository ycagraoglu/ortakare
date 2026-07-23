import { createTelemetryContext, reportTelemetry } from "@/shared/observability/telemetry";
import type { WebVitalTelemetryEvent } from "@/shared/observability/telemetry-types";

type WebVitalName = WebVitalTelemetryEvent["metric"];
type WebVitalRating = WebVitalTelemetryEvent["rating"];

function rateMetric(name: WebVitalName, value: number): WebVitalRating {
  const thresholds: Record<WebVitalName, readonly [number, number]> = {
    CLS: [0.1, 0.25],
    FCP: [1_800, 3_000],
    INP: [200, 500],
    LCP: [2_500, 4_000],
    TTFB: [800, 1_800],
  };
  const [good, poor] = thresholds[name];
  if (value <= good) return "good";
  if (value <= poor) return "needs-improvement";
  return "poor";
}

function report(name: WebVitalName, value: number): void {
  reportTelemetry({
    ...createTelemetryContext(),
    type: "web-vital",
    level: "info",
    metric: name,
    value: Number(value.toFixed(name === "CLS" ? 4 : 0)),
    rating: rateMetric(name, value),
  });
}

export function observeWebVitals(): () => void {
  const observers: PerformanceObserver[] = [];

  const observe = (type: string, callback: PerformanceObserverCallback) => {
    if (!PerformanceObserver.supportedEntryTypes.includes(type)) return;
    const observer = new PerformanceObserver(callback);
    observer.observe({ type, buffered: true });
    observers.push(observer);
  };

  observe("paint", (list) => {
    const fcp = list.getEntries().find((entry) => entry.name === "first-contentful-paint");
    if (fcp) report("FCP", fcp.startTime);
  });

  observe("largest-contentful-paint", (list) => {
    const latest = list.getEntries().at(-1);
    if (latest) report("LCP", latest.startTime);
  });

  let clsValue = 0;
  observe("layout-shift", (list) => {
    for (const entry of list.getEntries() as Array<PerformanceEntry & { value: number; hadRecentInput: boolean }>) {
      if (!entry.hadRecentInput) clsValue += entry.value;
    }
  });

  observe("event", (list) => {
    const longest = Math.max(...list.getEntries().map((entry) => entry.duration), 0);
    if (longest > 0) report("INP", longest);
  });

  const navigation = performance.getEntriesByType("navigation")[0] as PerformanceNavigationTiming | undefined;
  if (navigation) report("TTFB", navigation.responseStart);

  const onVisibilityChange = () => {
    if (document.visibilityState === "hidden" && clsValue > 0) report("CLS", clsValue);
  };
  document.addEventListener("visibilitychange", onVisibilityChange);

  return () => {
    observers.forEach((observer) => observer.disconnect());
    document.removeEventListener("visibilitychange", onVisibilityChange);
  };
}
