import { ApiError } from "@/shared/api";

const MAX_QUERY_RETRY_COUNT = 2;

export function shouldRetryQuery(failureCount: number, error: unknown): boolean {
  if (failureCount >= MAX_QUERY_RETRY_COUNT) return false;
  if (!(error instanceof ApiError)) return false;

  return error.kind === "network" || error.kind === "timeout" || error.kind === "server";
}

export function queryRetryDelay(attemptIndex: number): number {
  return Math.min(1_000 * 2 ** attemptIndex, 8_000);
}
